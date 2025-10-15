namespace paperless.DAL.Repositories;

using paperless.DAL.Models;

public interface IDocumentRepository
{
    Task CreateOrUpdateAsync(Document document);
    Task<List<Document>> ReadAllAsync();
    Task<Document?> ReadByIdAsync(Guid id);
    Task<List<Document>> ReadByTitleAsync(string title);
    Task DeleteAllAsync();
    Task DeleteByIdAsync(Guid id);
}