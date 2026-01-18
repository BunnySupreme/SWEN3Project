namespace Paperless.BatchProcessor.DAL.Repositories;

public interface IDocumentRepository
{
    Task<bool> UpdateAccessCountAsync(Guid documentId, int accessCount, CancellationToken ct = default);
}