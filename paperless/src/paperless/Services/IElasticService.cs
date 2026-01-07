using Paperless.Api;
using Paperless.Search.Models;

namespace Paperless.Services
{
    public interface IElasticService
    {
        Task<IReadOnlyList<DocumentReadDto>> SearchAsync(string userId, string searchTerm, CancellationToken ct);
        Task<bool> CreateIndexAsync(DocumentSearchModel document, CancellationToken ct);
        Task<bool> UpdateIndexAsync(DocumentSearchModel document, CancellationToken ct);
        Task<bool> DeleteIndexAsync(Guid id, CancellationToken ct);
    }
}
