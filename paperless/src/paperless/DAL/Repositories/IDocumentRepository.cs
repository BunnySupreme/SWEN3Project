using Paperless.DAL.Models;
using System;

namespace Paperless.DAL.Repositories;

public interface IDocumentRepository
{
    Task CreateOrUpdateAsync(DocumentModel document, CancellationToken ct = default);
    Task<DocumentModel?> ReadByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<DocumentModel?> ReadByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DocumentModel>> ReadAllAsync(CancellationToken ct = default);
    Task<List<DocumentModel>> ReadListAsync(Guid userId, string? title, int skip, int take, CancellationToken ct = default);
    Task<int> DeleteAllAsync(CancellationToken ct = default);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsForUserAsync(Guid id, Guid userId, CancellationToken ct = default);
}