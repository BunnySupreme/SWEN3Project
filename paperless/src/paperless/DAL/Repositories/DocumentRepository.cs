using paperless.DAL.Models;

namespace paperless.DAL.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        #region Constructors
        public DocumentRepository(DataContext db)
        { 
            _db = db;
        }
        #endregion

        #region DataContext
        private readonly DataContext _db;
        #endregion

        #region CRUD Operations
        public void CreateOrUpdate(Document document)
        {
            Document? dbDocument = _db.Documents.FirstOrDefault(d => d.Id == document.Id);
            if (dbDocument == null)
            {
                _db.Documents.Add(document);
            }
            else
            {
                dbDocument.Update(document.Title, document.Content, document.Summary, document.Tags);
            }
            _db.SaveChanges();
        }

        public List<Document> ReadAll()
        {
            return _db.Documents.ToList();
        }

        public Document? ReadById(Guid id)
        {
            return _db.Documents.FirstOrDefault(d => d.Id == id);
        }

        public List<Document> ReadByTitle(string title)
        {
            return _db.Documents
                .Where(d => d.Title.Contains(title))
                .ToList();
        }

        public void DeleteAll()
        {
            _db.Documents.RemoveRange(_db.Documents);
            _db.SaveChanges();
        }

        public void DeleteById(Guid id)
        {
            Document? document = _db.Documents.FirstOrDefault(d => d.Id == id);
            if (document != null)
            {
                _db.Documents.Remove(document);
                _db.SaveChanges();
            }
        }
        #endregion
    }
}
