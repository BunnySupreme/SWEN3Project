using System.Net.Http.Json;
using log4net;

namespace Paperless.OcrWorker
{
    public class GeminiSummarizerClient : IGeminiSummarizerClient
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;
        #endregion

        #region Constructors
        public GeminiSummarizerClient(HttpClient client)
        {
            _httpClient = client;
            _logger = LogManager.GetLogger(typeof(GeminiSummarizerClient));
        }
        #endregion

        #region Methods
        public async Task<string> SummarizeTextAsync(string text, CancellationToken ct)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (!ct.IsCancellationRequested && retryCount < maxRetries)
            {
                try
                {
                    var request = new { Text = text };
                    var response = await _httpClient.PostAsJsonAsync("/api/gemini/summarize", request, ct);

                    response.EnsureSuccessStatusCode();

                    var summaryJson = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);

                    if (summaryJson == null || !summaryJson.ContainsKey("summary"))
                    {
                        throw new Exception("Invalid response from Gemini summarizer.");
                    }

                    var summary = summaryJson["summary"];

                    _logger.Info($"SummarizerClient successfully received summary from Gemini summarizer: {summary}");

                    return summary;
                }
                catch (Exception)
                {
                    _logger.Error($"GeminiSummarizer failed to return summary (attempt {retryCount + 1}/{maxRetries})");
                    retryCount++;

                    if (retryCount >= maxRetries)
                    {
                        _logger.Error("GeminiSummarizerClient max retry attempts reached. Failing summarization and returning default summary info.");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
            return "Summary could not be created.";
        }
        #endregion
    }
}
