namespace Paperless.BatchProcessor.Services.Records
{
    public sealed record AccessEntry(
        Guid DocumentId,
        int AccessCount);
}
