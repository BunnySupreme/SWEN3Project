using AutoMapper;
using FluentValidation;
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
    private readonly IValidator<DocumentCreateDto> _createValidator;
    private readonly IValidator<DocumentUpdateDto> _updateValidator;
    private readonly IRabbitProducerService _rabbitProducer;
    #endregion

    #region Constructors
    public DocumentService(
        IDocumentRepository repo,
        DataContext db,
        IMapper mapper,
        IValidator<DocumentCreateDto> createValidator,
        IValidator<DocumentUpdateDto> updateValidator,
        IRabbitProducerService rabbitProducer)
    {
        _repo = repo;
        _db = db;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _rabbitProducer = rabbitProducer;
    }
    #endregion

    // ─────────────────────────────────────────────
    // LIST
    // ─────────────────────────────────────────────
    public async Task<IReadOnlyList<DocumentReadDto>> ListAsync(
        string? title, int skip, int take, CancellationToken ct = default)
    {
        var docs = string.IsNullOrWhiteSpace(title)
            ? await _repo.ReadAllAsync(ct)
            : await _repo.ReadByTitleAsync(title, ct);

        return docs
            .OrderByDescending(d => d.UploadedAt)
            .ThenByDescending(d => d.Title)
            .Skip(skip)
            .Take(take)
            .Select(d => _mapper.Map<DocumentReadDto>(d))
            .ToList()
            .AsReadOnly();
    }

    // ─────────────────────────────────────────────
    // GET
    // ─────────────────────────────────────────────
    public async Task<DocumentReadDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.ReadByIdAsync(id, ct);

        // ADD: Fetch associated file from MinIO (if necessary for this operation)

        return doc is null ? null : _mapper.Map<DocumentReadDto>(doc);
    }

    // ─────────────────────────────────────────────
    // CREATE (JSON)
    // ─────────────────────────────────────────────
    public async Task<DocumentReadDto> CreateAsync(DocumentCreateDto dto, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var entity = _mapper.Map<DocumentModel>(dto);

        await _repo.CreateOrUpdateAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return _mapper.Map<DocumentReadDto>(entity);
    }


    // ─────────────────────────────────────────────
    // UPLOAD (MULTIPART)
    // ─────────────────────────────────────────────
    public async Task<DocumentReadDto> UploadAsync(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("File is required.");

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        // Build DTO from uploaded file
        var dto = new DocumentCreateDto
        (
            Title: Path.GetFileName(file.FileName),
            Summary: string.Empty,
            Tags: Array.Empty<string>()
        );

        var uploadValidation = await _createValidator.ValidateAsync(dto, ct);
        if (!uploadValidation.IsValid)
            throw new ValidationException(uploadValidation.Errors);

        // DB (Initial, no summary, summary will be handled via RabbitConsumerService)
        var created = await CreateAsync(dto, ct); // CreateAsync validates again (mocks return valid) and persists

        // OCR Queue (Produce message for OCR)
        MessageModel message = new MessageModel
        {
            DocumentId = created.Id,
            DocumentTitle = created.Title,
            QueuedAt = DateTimeOffset.UtcNow
        };
        await _rabbitProducer.RunAsync(message, ct);

        return created;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────
    public async Task<bool> UpdateAsync(DocumentUpdateDto dto, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var entity = await _repo.ReadByIdAsync(dto.Id, ct);
        if (entity is null) return false;

        entity.Update(
            title: dto.Title,
            summary: dto.Summary ?? string.Empty,
            tags: ToCsv(dto.Tags));

        await _repo.CreateOrUpdateAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ─────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.ReadByIdAsync(id, ct);
        if (entity is null) return false;

        await _repo.DeleteByIdAsync(id, ct);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    private static string ToCsv(IReadOnlyList<string>? tags) =>
        tags is null || tags.Count == 0
            ? string.Empty
            : string.Join(',', tags);
}
