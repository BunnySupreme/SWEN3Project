using Paperless.DAL.Models;
using Paperless.DAL.Repositories;
using Paperless.Api;
using AutoMapper;

namespace Paperless.Services;

public sealed class DocumentService : IDocumentService
{
    #region Fields
    private readonly IDocumentRepository _repo;
    private readonly IMapper _mapper;
    #endregion

    #region Constructors
    public DocumentService(IDocumentRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }
    #endregion

    #region Methods
    public Task<IReadOnlyList<DocumentReadDto>> ListAsync(
        string? title, int skip, int take, CancellationToken ct = default)
    {
        var docs = string.IsNullOrWhiteSpace(title)
            ? _repo.ReadAll()
            : _repo.ReadByTitle(title);

        var page = docs
            .OrderByDescending(d => d.UploadedAt)
            .ThenByDescending(d => d.Title)
            .Skip(skip)
            .Take(take)
            .Select(_mapper.Map<DocumentReadDto>)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<DocumentReadDto>>(page);
    }

    public Task<DocumentReadDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var doc = _repo.ReadById(id);
        return Task.FromResult(doc is null ? null : _mapper.Map<DocumentReadDto>(doc));
    }

    public Task<DocumentReadDto> CreateAsync(DocumentCreateDto dto, CancellationToken ct = default)
    {
        var entity = _mapper.Map<DocumentModel>(dto);

        _repo.CreateOrUpdate(entity);

        var read = _mapper.Map<DocumentReadDto>(entity);
        return Task.FromResult(read);
    }

    public Task<bool> UpdateAsync(DocumentUpdateDto dto, CancellationToken ct = default)
    {
        var entity = _repo.ReadById(dto.Id);
        if (entity is null) return Task.FromResult(false);

        _mapper.Map(dto, entity);

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
    #endregion
}
