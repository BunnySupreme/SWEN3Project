// Paperless.Services/DocumentService.cs
using paperless.DAL.Models;
using paperless.DAL.Repositories;
using Paperless.Api.Contracts;

namespace Paperless.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repo;

    public DocumentService(IDocumentRepository repo) => _repo = repo;

    public Task<IReadOnlyList<DocumentReadDto>> ListAsync(
        string? title, int skip, int take, CancellationToken ct = default)
    {
        var docs = string.IsNullOrWhiteSpace(title)
            ? _repo.ReadAll()
            : _repo.ReadByTitle(title);

        var page = docs
            .OrderByDescending(d => d.CreationDate)
            .ThenByDescending(d => d.Title)
            .Skip(skip)
            .Take(take)
            .Select(d => MapToReadDto(d))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<DocumentReadDto>>(page);
    }

    public Task<DocumentReadDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var doc = _repo.ReadById(id);
        return Task.FromResult(doc is null ? null : MapToReadDto(doc));
    }

    public Task<DocumentReadDto> CreateAsync(DocumentCreateDto dto, CancellationToken ct = default)
    {
        var entity = new Document();
        entity.Update(
            title: dto.FileName,
            content: string.Empty,
            summary: dto.Summary ?? string.Empty,
            tags: ToCsv(dto.Tags)
        );

        _repo.CreateOrUpdate(entity);

        var read = MapToReadDto(entity, dto.ContentType ?? "application/pdf");
        return Task.FromResult(read);
    }

    public Task<bool> UpdateAsync(DocumentUpdateDto dto, CancellationToken ct = default)
    {
        var entity = _repo.ReadById(dto.Id);
        if (entity is null) return Task.FromResult(false);

        entity.Update(
            title: dto.FileName,
            content: entity.Content,
            summary: dto.Summary ?? string.Empty,
            tags: ToCsv(dto.Tags)
        );

        _repo.CreateOrUpdate(entity);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = _repo.ReadById(id);
        if (entity is null) return Task.FromResult(false);

        _repo.DeleteById(id);
        return Task.FromResult(true);
    }


    private static string ToCsv(IReadOnlyList<string>? tags) =>
        (tags is null || tags.Count == 0)
            ? string.Empty
            : string.Join(',', tags);

    private static DocumentReadDto MapToReadDto(Document d, string? contentType = null) =>
        new(
            Id: d.Id,
            FileName: d.Title,
            ContentType: contentType ?? "application/pdf",
            UploadedAt: d.CreationDate,
            Summary: d.Summary,
            Tags: string.IsNullOrWhiteSpace(d.Tags)
                ? Array.Empty<string>()
                : d.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        );
}
