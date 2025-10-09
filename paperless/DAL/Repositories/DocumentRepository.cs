using paperless.DAL.Models;

namespace paperless.DAL.Repositories
{
    public class DocumentRepository
    {
        #region Constructors
        public DocumentRepository() { }
        #endregion

        #region CRUD Operations
        public void Create(DataContext db, Document document) // If duplicate documents are unwanted, have method look for title or content and refuse creation if they already exist
        {
            db.Documents.Add(document);
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

        public Document? ReadByTitle(DataContext db, string title)
        {
            return db.Documents.FirstOrDefault(d => d.Title == title);
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

        public void DeleteByTitle(DataContext db, string title)
        {
            Document? document = db.Documents.FirstOrDefault(d => d.Title == title);
            if (document != null)
            {
                db.Documents.Remove(document);
                db.SaveChanges();
            }
        }
        #endregion
    }
}
