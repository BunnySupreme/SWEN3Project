namespace paperless.DAL.Repositories;

using paperless.DAL.Models;

public interface IDocumentRepository
{
    void CreateOrUpdate(Document document);
    List<Document> ReadAll();
    Document? ReadById(Guid id);
    List<Document> ReadByTitle(string title);
    void DeleteAll();
    void DeleteById(Guid id);
}