using AutoMapper;
using Paperless.Api;
using Paperless.DAL;
using Paperless.DAL.Models;
using Paperless.DAL.Repositories;

namespace Paperless.Services;

public sealed class DocumentService : IDocumentService
{
    #region Fields
    private readonly IDocumentRepository _repo;
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    #endregion

    #region Constructors
    public DocumentService(IDocumentRepository repo, DataContext db, IMapper mapper)
    {
        _repo = repo;
        _db = db;
        _mapper = mapper;
    }
    #endregion

    #region Methods
    // ---------------------------------------------
    // LIST
    // ---------------------------------------------
    public async Task<IReadOnlyList<DocumentReadDto>> ListAsync(
        string? title, int skip, int take, CancellationToken ct = default)
    {
        var docs = string.IsNullOrWhiteSpace(title)
            ? await _repo.ReadAllAsync()
            : await _repo.ReadByTitleAsync(title);

        return docs
            .OrderByDescending(d => d.UploadedAt)
            .ThenByDescending(d => d.Title)
            .Skip(skip)
            .Take(take)
            .Select(_mapper.Map<DocumentReadDto>)
            .ToList()
            .AsReadOnly();
    }

    // ---------------------------------------------
    // GET
    // ---------------------------------------------
    public async Task<DocumentReadDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.ReadByIdAsync(id, ct);

        return doc is null ? null : _mapper.Map<DocumentReadDto>(doc);
    }

    // ---------------------------------------------
    // CREATE (JSON)
    // ---------------------------------------------
    public async Task<DocumentReadDto> CreateAsync(DocumentCreateDto dto, CancellationToken ct = default)
    {
        var entity = _mapper.Map<DocumentModel>(dto);

        await _repo.CreateOrUpdateAsync(entity, ct);
        await _db.SaveChangesAsync();

        return _mapper.Map<DocumentReadDto>(entity);
    }

    // ---------------------------------------------
    // UPLOAD (MULTIPART)
    // ---------------------------------------------
    public async Task<DocumentReadDto> UploadAsync(IFormFile file, CancellationToken ct)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        byte[] fileBytes = memoryStream.ToArray();
        string fileContent = Convert.ToBase64String(fileBytes);

        var dto = new DocumentCreateDto
        (
            Title: Path.GetFileName(file.FileName),
            Summary: string.Empty,
            Tags: Array.Empty<string>()
        );

        var created = await CreateAsync(dto, ct);
        // (Future: save file bytes / upload to MinIO here)
        return created;
    }

    // ---------------------------------------------
    // UPDATE
    // ---------------------------------------------
    public async Task<bool> UpdateAsync(DocumentUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.ReadByIdAsync(dto.Id, ct);
        if (entity is null) return false;

        _mapper.Map(dto, entity);

        await _repo.CreateOrUpdateAsync(entity, ct);
        await _db.SaveChangesAsync();
        return true;
    }

    // ---------------------------------------------
    // DELETE
    // ---------------------------------------------
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.ReadByIdAsync(id, ct);
        if (entity is null) return false;

        await _repo.DeleteByIdAsync(id, ct);
        await _db.SaveChangesAsync();
        return true;
    }
    #endregion
}
