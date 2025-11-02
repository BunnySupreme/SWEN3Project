using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Models;

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
        public async Task CreateOrUpdateAsync(DocumentModel document, CancellationToken ct = default)
        {
            var existing = await _db.Documents
                .FirstOrDefaultAsync(d => d.Id == document.Id, ct);

            if (existing is null)
            {
                await _db.Documents.AddAsync(document, ct);
            }
            else
            {
                existing.Update(document.Title, document.Summary, document.Tags);
            }
        }

        public Task<List<DocumentModel>> ReadAllAsync(CancellationToken ct = default) =>
        _db.Documents.AsNoTracking().ToListAsync(ct);

        public Task<DocumentModel?> ReadByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

        public Task<List<DocumentModel>> ReadByTitleAsync(string title, CancellationToken ct = default) =>
        _db.Documents
           .AsNoTracking()
           .Where(d => EF.Functions.Like(d.Title, $"%{title}%"))
           .ToListAsync(ct);

        public Task<int> DeleteAllAsync(CancellationToken ct = default) =>
        _db.Documents.ExecuteDeleteAsync(ct);

        public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default)
        {
            var deleted = await _db.Documents
                .Where(d => d.Id == id)
                .ExecuteDeleteAsync(ct);
            if (deleted > 0) return true;
            return false;
        }
        #endregion
    }
}