namespace Paperless.BatchProcessor.Services
{
    public interface IXmlProcessorService
    {
        Task RunOnceAsync(string inputDir, string archiveDir, string errorDir, string filePattern, CancellationToken ct = default);
    }
}
