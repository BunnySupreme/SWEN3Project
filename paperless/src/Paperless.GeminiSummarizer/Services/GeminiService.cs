using System.Net;
using System.Text;
using System.Text.Json;

namespace Paperless.GeminiSummarizer.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http = new();
        private readonly string _apiKey =
            Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? throw new Exception("GEMINI_API_KEY missing");

        public async Task<string> SummarizeTextAsync(string text, CancellationToken ct = default)
        {
            var prompt = """
                Create a short summary of the following text. Have a neutral tone, and in your response,
                do not say "this is your summary" but instead give the summary directly, so that the response
                can be input into a database directly. Make sure to be very brief, 2-3 short paragraphs at most

                """ + text;

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    topK = 40,
                    topP = 0.9,
                    maxOutputTokens = 1024
                }
            };

            var body = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("X-goog-api-key", _apiKey);

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync(
                    "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
                    body,
                    ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new HttpRequestException("Gemini unreachable or timed out.", ex);
            }

            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode >= 500 ||
                    response.StatusCode == HttpStatusCode.TooManyRequests ||
                    response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    throw new HttpRequestException(
                        $"Retryable Gemini upstream failure ({(int)response.StatusCode}). Content: {content}");
                }
                throw new InvalidOperationException(
                    $"Non-retryable Gemini error ({(int)response.StatusCode}). Content: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var summaryText =
                root.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            if (string.IsNullOrWhiteSpace(summaryText))
                throw new HttpRequestException("Gemini returned empty summary, treat as temporary failure.");
            Console.WriteLine("SUMMARY >>> " + summaryText);
            return summaryText;
        }
    }
}
