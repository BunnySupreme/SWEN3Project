namespace paperless.DAL.Repositories;

using paperless.DAL.Models;

public interface IDocumentRepository
{
    Task CreateOrUpdateAsync(CancellationToken ct);
    Task<List<Document>> ReadAllAsync(CancellationToken ct);
    Task<Document?> ReadByIdAsync(Guid id, CancellationToken ct);
    Task<List<Document>> ReadByTitleAsync(string title, CancellationToken ct);
    Task DeleteAllAsync(CancellationToken ct);
    Task DeleteByIdAsync(Guid id, CancellationToken ct);
}