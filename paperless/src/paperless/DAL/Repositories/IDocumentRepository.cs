namespace paperless.DAL.Repositories;

using paperless.DAL.Models;

public interface IDocumentRepository
{
    Task CreateOrUpdateAsync(Document document, CancellationToken ct = default);
    Task<List<Document>> ReadAllAsync(CancellationToken ct = default);
    Task<Document?> ReadByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Document>> ReadByTitleAsync(string title, CancellationToken ct = default);
    Task<int> DeleteAllAsync(CancellationToken ct = default);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default);
}