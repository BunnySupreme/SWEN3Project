namespace Paperless.OcrWorker
{
    public interface IGeminiSummarizerClient
    {
        Task<string> SummarizeTextAsync(string text, CancellationToken cancellationToken);
    }
}
