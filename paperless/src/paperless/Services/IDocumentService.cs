using Paperless.Api;

namespace Paperless.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentReadDto>> ListAsync(Guid userId, string? title, int skip, int take, CancellationToken ct = default);
    Task<DocumentReadDto?> GetAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<MemoryStream> DownloadAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<DocumentReadDto> CreateAsync(Guid userId, DocumentCreateDto dto, CancellationToken ct = default);
    Task<DocumentReadDto> UploadAsync(Guid userId, IFormFile file, DocumentCreateDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(Guid userId, DocumentUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
