// Paperless.Services/DocumentService.cs
using paperless.DAL;
using log4net;
using paperless.DAL.Models;
using paperless.DAL.Repositories;
using Paperless.Api.Contracts;

namespace Paperless.Services;

public sealed class DocumentService : IDocumentService
{
    #region Fields
    private readonly IDocumentRepository _repo;
    private readonly DataContext _db;
    #endregion

    #region Constructors
    public DocumentService(IDocumentRepository repo, DataContext db)
    {
        _repo = repo;
        _db = db;
    }
    #endregion

    #region Methods
    public async Task<IReadOnlyList<DocumentReadDto>> ListAsync(
        string? title, int skip, int take, CancellationToken ct = default)
    {
        var docs = string.IsNullOrWhiteSpace(title)
            ? await _repo.ReadAllAsync()
            : await _repo.ReadByTitleAsync(title);

        var page = docs
            .OrderByDescending(d => d.CreationDate)
            .ThenByDescending(d => d.Title)
            .Skip(skip)
            .Take(take)
            .Select(d => MapToReadDto(d))
            .ToList()
            .AsReadOnly();

        return page;
    }

    public async Task<DocumentReadDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.ReadByIdAsync(id, ct);
        return doc is null ? null : MapToReadDto(doc);
    }

    public async Task<DocumentReadDto> CreateAsync(DocumentCreateDto dto, CancellationToken ct = default)
    {
        var entity = new Document();
        entity.Update(
            title: dto.FileName,
            content: string.Empty,
            summary: dto.Summary ?? string.Empty,
            tags: ToCsv(dto.Tags)
        );

        await _repo.CreateOrUpdateAsync(entity, ct);
        await _db.SaveChangesAsync();

        var read = MapToReadDto(entity, dto.ContentType ?? "application/pdf");
        return read;
    }

    public async Task<bool> UpdateAsync(DocumentUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.ReadByIdAsync(dto.Id, ct);
        if (entity is null) return false;

        entity.Update(
            title: dto.FileName,
            content: entity.Content,
            summary: dto.Summary ?? string.Empty,
            tags: ToCsv(dto.Tags)
        );

        await _repo.CreateOrUpdateAsync(entity, ct);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.ReadByIdAsync(id, ct);
        if (entity is null) return false;

        await _repo.DeleteByIdAsync(id, ct);
        await _db.SaveChangesAsync();
        return true;
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
    #endregion
}
