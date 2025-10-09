namespace Paperless.Services;

using System.Collections.Generic;
using System.Linq;
using Paperless.Api.Contracts;

///  Replace with a repository-backed
/// implementation once the DAL is ready.
public sealed class DocumentService : IDocumentService
{
    private readonly List<DocumentReadDto> _documents = new();
    private readonly object _mutex = new();

    public Task<IReadOnlyList<DocumentReadDto>> ListAsync(
        string? title,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        IEnumerable<DocumentReadDto> query = _documents;

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(d =>
                d.FileName.Contains(title, StringComparison.OrdinalIgnoreCase));
        }

        var page = query
            .Skip(skip)
            .Take(take)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<DocumentReadDto>>(page);
    }

    public Task<DocumentReadDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var document = _documents.FirstOrDefault(d => d.Id == id);
        return Task.FromResult(document);
    }

    public Task<DocumentReadDto> CreateAsync(
        DocumentCreateDto dto,
        CancellationToken ct = default)
    {
        var newDocument = new DocumentReadDto(
            Id: Guid.NewGuid(),
            FileName: dto.FileName,
            ContentType: string.IsNullOrWhiteSpace(dto.ContentType)
                ? "application/pdf"
                : dto.ContentType,
            UploadedAt: DateTimeOffset.UtcNow,
            Summary: dto.Summary,
            Tags: dto.Tags ?? Array.Empty<string>());

        lock (_mutex)
        {
            _documents.Add(newDocument);
        }

        return Task.FromResult(newDocument);
    }

    public Task<bool> UpdateAsync(
        DocumentUpdateDto dto,
        CancellationToken ct = default)
    {
        lock (_mutex)
        {
            var index = _documents.FindIndex(d => d.Id == dto.Id);
            if (index < 0)
                return Task.FromResult(false);

            var current = _documents[index];

            var updated = current with
            {
                FileName = dto.FileName,
                ContentType = string.IsNullOrWhiteSpace(dto.ContentType)
                    ? current.ContentType
                    : dto.ContentType,
                Summary = dto.Summary,
                Tags = dto.Tags ?? Array.Empty<string>()
            };

            _documents[index] = updated;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        lock (_mutex)
        {
            var removedCount = _documents.RemoveAll(d => d.Id == id);
            return Task.FromResult(removedCount > 0);
        }
    }
}
