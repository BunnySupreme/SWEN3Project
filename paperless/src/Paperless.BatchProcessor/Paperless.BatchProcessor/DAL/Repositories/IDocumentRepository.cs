using Paperless.DAL.Models;
using System;

namespace Paperless.DAL.Repositories;

public interface IDocumentRepository
{
    Task<bool> UpdateAccessCountAsync(Guid documentId, int accessCount, CancellationToken ct = default);
}