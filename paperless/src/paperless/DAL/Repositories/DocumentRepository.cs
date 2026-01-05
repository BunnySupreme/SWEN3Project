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

        public Task<DocumentModel?> ReadByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

        public Task<List<DocumentModel>> ReadListAsync(Guid userId, string? title, int skip, int take, CancellationToken ct = default)
        {
            IQueryable<DocumentModel> q = _db.Documents.AsNoTracking().Where(d => d.UserId == userId);

            if (!string.IsNullOrWhiteSpace(title))
            {
                q = q.Where(d => EF.Functions.ILike(d.Title, $"%{title}%"));
            }

            return q.OrderByDescending(d => d.UploadedAt)
                    .ThenByDescending(d => d.Title)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(ct);
        }

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

        public async Task<bool> ExistsForUserAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        await _db.Documents.AsNoTracking().AnyAsync(d => d.Id == id && d.UserId == userId, ct);
        #endregion
    }
}