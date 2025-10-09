using paperless.DAL.Models;

namespace paperless.DAL.Repositories
{
    public class DocumentRepository
    {
        #region Constructors
        public DocumentRepository() { }
        #endregion

        #region CRUD Operations
        public void CreateOrUpdate(DataContext db, Document document)
        {
            Document? dbDocument = db.Documents.FirstOrDefault(d => d.Id == document.Id);
            if (dbDocument == null)
            {
                db.Documents.Add(document);
            }
            else
            {
                dbDocument.Title = document.Title;
                dbDocument.Content = document.Content;
                dbDocument.Summary = document.Summary;
                dbDocument.Tags = document.Tags;
            }
            db.SaveChanges();
        }

        public List<Document> ReadAll(DataContext db)
        {
            return db.Documents.ToList();
        }

        public Document? ReadyById(DataContext db, Guid id)
        {
            return db.Documents.FirstOrDefault(d => d.Id == id);
        }

        public List<Document> ReadByTitle(DataContext db, string title)
        {
            return db.Documents
                .Where(d => d.Title.Contains(title))
                .ToList();
        }

        public void DeleteAll(DataContext db)
        {
            db.Documents.RemoveRange(db.Documents);
            db.SaveChanges();
        }

        public void DeleteById(DataContext db, Guid id)
        {
            Document? document = db.Documents.FirstOrDefault(d => d.Id == id);
            if (document != null)
            {
                db.Documents.Remove(document);
                db.SaveChanges();
            }
        }
        #endregion
    }
}
