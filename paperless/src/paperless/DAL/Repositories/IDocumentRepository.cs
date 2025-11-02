using Paperless.DAL.Models;

namespace Paperless.DAL.Repositories;

public interface IDocumentRepository
{
    Task CreateOrUpdateAsync(DocumentModel document, CancellationToken ct = default);
    Task<List<DocumentModel>> ReadAllAsync(CancellationToken ct = default);
    Task<DocumentModel?> ReadByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DocumentModel>> ReadByTitleAsync(string title, CancellationToken ct = default);
    Task<int> DeleteAllAsync(CancellationToken ct = default);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default);
}