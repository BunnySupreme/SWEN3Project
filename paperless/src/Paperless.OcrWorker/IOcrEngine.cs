namespace Paperless.OcrWorker
{
    public interface IOcrEngine
    {
        Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken ct = default);
    }

}
