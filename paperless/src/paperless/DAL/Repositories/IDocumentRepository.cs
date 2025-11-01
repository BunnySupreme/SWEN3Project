namespace Paperless.DAL.Repositories;

using Paperless.DAL.Models;

public interface IDocumentRepository
{
    void CreateOrUpdate(DocumentModel document);
    List<DocumentModel> ReadAll();
    DocumentModel? ReadById(Guid id);
    List<DocumentModel> ReadByTitle(string title);
    void DeleteAll();
    void DeleteById(Guid id);
}