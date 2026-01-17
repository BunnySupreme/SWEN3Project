using Microsoft.EntityFrameworkCore;
using Paperless.BatchProcessor.DAL;

namespace Paperless.DAL.Repositories
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
        public async Task<bool> UpdateAccessCountAsync(Guid documentId, int accessCount, CancellationToken ct = default)
        {
            var existing = await _db.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId, ct);

            if (existing is null)
            {
                // Document not found
                return false;
            }
            else
            {
                // Add the amount of accesses to total access count
                existing.AccessCount += accessCount;
                return true;
            }
        }
        #endregion
    }
}