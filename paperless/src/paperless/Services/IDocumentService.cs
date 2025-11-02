namespace Paperless.Services;
using Paperless.Api;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentReadDto>> ListAsync(string? title, int skip, int take, CancellationToken ct = default);
    Task<DocumentReadDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<DocumentReadDto> CreateAsync(DocumentCreateDto dto, CancellationToken ct = default);
    Task<DocumentReadDto> UploadAsync(IFormFile file, CancellationToken ct);
    Task<bool> UpdateAsync(DocumentUpdateDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
