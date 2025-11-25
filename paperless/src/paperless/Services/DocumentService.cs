using AutoMapper;
using FluentValidation;
using log4net;
using Microsoft.AspNetCore.Http.HttpResults;
using Minio;
using Minio.DataModel.Args;
using Paperless.Api;
using Paperless.DAL;
using Paperless.DAL.Models;
using Paperless.DAL.Repositories;

namespace Paperless.Services;

public sealed class DocumentService : IDocumentService
{
    #region Fields
    private readonly IDocumentRepository _repo;
    private readonly IMinioClient _minioClient;
    private const string BucketName = "documents";
    private readonly DataContext _db;
    private readonly IMapper _mapper;
    private readonly IValidator<DocumentCreateDto> _createValidator;
    private readonly IValidator<DocumentUpdateDto> _updateValidator;
    private readonly IRabbitProducerService _rabbitProducer;
    private readonly ILog _logger;
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
        _minioClient = new MinioClient()
            .WithEndpoint("paperless-minio", 9000)
            .WithCredentials("paperless", Configuration.MinioPassword)
            .WithSSL(false)
            .Build();
        _db = db;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _rabbitProducer = rabbitProducer;
        _logger = LogManager.GetLogger(typeof(DocumentService));
    }
    #endregion

    // ─────────────────────────────────────────────
    // LIST
    // ─────────────────────────────────────────────
    public async Task<IReadOnlyList<DocumentReadDto>> ListAsync(
        string? title, int skip, int take, CancellationToken ct = default)
    {
        _logger.Info($"Listing document info from DB. Title filter: '{title ?? "null"}', Skip: {skip}, Take: {take}");

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
        _logger.Info($"Fetching document info from DB with ID: {id}");

        var doc = await _repo.ReadByIdAsync(id, ct);

        return doc is null ? null : _mapper.Map<DocumentReadDto>(doc);
    }

    // ─────────────────────────────────────────────
    // DOWNLOAD
    // ─────────────────────────────────────────────
    public async Task<MemoryStream> DownloadAsync(Guid id, CancellationToken ct = default)
    {
        _logger.Info($"Downloading document from MinIO with ID: {id}");

        var fileName = id.ToString();
        var memoryStream = new MemoryStream();

        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(fileName)
            .WithCallbackStream(stream =>
            {
                stream.CopyTo(memoryStream);
            }));

        memoryStream.Position = 0;

        return memoryStream;
    }

    // ─────────────────────────────────────────────
    // CREATE
    // ─────────────────────────────────────────────
    public async Task<DocumentReadDto> CreateAsync(DocumentCreateDto dto, CancellationToken ct = default)
    {
        _logger.Info($"Creating new document with Title: '{dto.Title}', Tags: '{dto.Tags}");

        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            _logger.Warn("Document creation validation failed.");
            throw new ValidationException(validation.Errors);
        }

        var entity = _mapper.Map<DocumentModel>(dto);

        await _repo.CreateOrUpdateAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return _mapper.Map<DocumentReadDto>(entity);
    }


    // ─────────────────────────────────────────────
    // UPLOAD
    // ─────────────────────────────────────────────
    public async Task<DocumentReadDto> UploadAsync(IFormFile file, DocumentCreateDto dto, CancellationToken ct)
    {
        // Logger
        _logger.Info($"Uploading new document from file: '{file?.FileName}'");

        // Validation
        if (file is null || file.Length == 0)
        {
            _logger.Warn("File upload failed: No file provided or file is empty.");
            throw new ValidationException("File is required.");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);

        var uploadValidation = await _createValidator.ValidateAsync(dto, ct);
        if (!uploadValidation.IsValid)
        {
            _logger.Warn("File upload validation failed.");
            throw new ValidationException(uploadValidation.Errors);
        }

        // DB (Initial, no summary, summary will be handled via RabbitConsumerService)
        var created = await CreateAsync(dto, ct); // CreateAsync validates again (mocks return valid) and persists

        // Minio (Store file)
        try
        {
            await EnsureBucketExists();

            var fileName = created.Id.ToString();
            await using var fileStream = file.OpenReadStream();

            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(file.Length));
        }
        catch (Exception ex)
        {
            _logger.Error("Storing file in Minio failed. ", ex);
        }

        // OCR Queue (Produce message for OCR)
        MessageModel message = new MessageModel
        {
            DocumentId = created.Id,
            DocumentTitle = created.Title,
            QueuedAt = DateTimeOffset.UtcNow
        };
        await _rabbitProducer.RunAsync(message, ct);

        // Done
        return created;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────
    public async Task<bool> UpdateAsync(DocumentUpdateDto dto, CancellationToken ct = default)
    {
        _logger.Info($"Updating document with ID: {dto.Id}");

        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            _logger.Warn("Document update validation failed.");
            throw new ValidationException(validation.Errors);
        }

        var entity = await _repo.ReadByIdAsync(dto.Id, ct);
        if (entity is null)
        {
            _logger.Warn($"Document with ID: {dto.Id} not found for update.");
            return false;
        }

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
        _logger.Info($"Deleting document with ID: {id}");

        var entity = await _repo.ReadByIdAsync(id, ct);
        if (entity is null)
        {
            _logger.Warn($"Document with ID: {id} not found for deletion.");
            return false;
        }

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

    private async Task EnsureBucketExists()
    {
        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
        if(!found)
        {
            _logger.Info($"Bucket '{BucketName}' not found. Creating new bucket.");
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
        }
    }
}
