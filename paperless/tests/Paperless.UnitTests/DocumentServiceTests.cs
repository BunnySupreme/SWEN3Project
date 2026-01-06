using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Moq;
using Paperless.Api;
using Paperless.DAL;
using Paperless.DAL.Models;
using Paperless.DAL.Repositories;
using Paperless.Services;

namespace Paperless.UnitTests;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _repoMock = new();
    private readonly DataContext _db;
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IValidator<DocumentCreateDto>> _createValidatorMock = new();
    private readonly Mock<IValidator<DocumentUpdateDto>> _updateValidatorMock = new();
    private readonly Mock<IRabbitProducerService> _rabbitProducerMock = new();
    private readonly Mock<IElasticService> _elasticServiceMock;
    private readonly DocumentService _service;

    private readonly Guid _userId = Guid.NewGuid();

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new DataContext(options);

        // Mapper: DocumentModel -> DocumentReadDto
        _mapperMock
            .Setup(m => m.Map<DocumentReadDto>(It.IsAny<DocumentModel>()))
            .Returns((DocumentModel d) =>
                new DocumentReadDto(
                    Id: d.Id,
                    Title: d.Title,
                    Summary: d.Summary,
                    Tags: string.IsNullOrWhiteSpace(d.Tags)
                        ? Array.Empty<string>()
                        : d.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    UploadedAt: d.UploadedAt
                )
            );

        // Mapper: DocumentCreateDto -> DocumentModel
        _mapperMock
            .Setup(m => m.Map<DocumentModel>(It.IsAny<DocumentCreateDto>()))
            .Returns((DocumentCreateDto dto) =>
            {
                var model = new DocumentModel
                {
                    Title = dto.Title,
                    Summary = dto.Summary ?? string.Empty,
                    Tags = dto.Tags is null ? string.Empty : string.Join(',', dto.Tags),
                    UploadedAt = DateTimeOffset.UtcNow
                };
                return model;
            });

        _elasticServiceMock = new Mock<IElasticService>();
        _service = new DocumentService(
            _repoMock.Object,
            _db,
            _mapperMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            _rabbitProducerMock.Object);
            _elasticServiceMock.Object);
    }

    [Fact]
    public async Task ListAsync_CallsRepoWithUserId_AndMapsResults()
    {
        // Arrange
        var docs = new List<DocumentModel>
        {
            new DocumentModel { Id = Guid.NewGuid(), Title = "A", Summary = "s", Tags = "t1", UploadedAt = DateTimeOffset.UtcNow, UserId = _userId },
            new DocumentModel { Id = Guid.NewGuid(), Title = "B", Summary = "s", Tags = "t2", UploadedAt = DateTimeOffset.UtcNow, UserId = _userId },
        };

        _repoMock
            .Setup(r => r.ReadListAsync(
                _userId,
                null,
                0,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);


        // Act
        var result = await _service.ListAsync(_userId, title: null, skip: 0, take: 50, ct: CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);

        _repoMock.Verify(r => r.ReadListAsync(_userId, null, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsDto_WhenFound()
    {
        // Arrange
        var doc = new DocumentModel
        {
            Id = Guid.NewGuid(),
            Title = "Some File",
            Summary = "s",
            Tags = "t",
            UploadedAt = DateTimeOffset.UtcNow,
            UserId = _userId
        };

        _repoMock
            .Setup(r => r.ReadByIdAndUserIdAsync(doc.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        // Act
        var result = await _service.GetAsync(_userId, doc.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Some File", result!.Title);
    }

    [Fact]
    public async Task CreateAsync_SetsUserId_Persists_AndReturnsDto()
    {
        // Arrange
        var dto = new DocumentCreateDto(
            Title: "NewFile",
            Summary: "sum",
            Tags: new List<string> { "tag1" }
        );

        _createValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repoMock
            .Setup(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(_userId, dto, CancellationToken.None);

        // Assert
        Assert.Equal("NewFile", result.Title);

        _repoMock.Verify(r =>
            r.CreateOrUpdateAsync(
                It.Is<DocumentModel>(m => m.UserId == _userId && m.Title == "NewFile"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsTrue_WhenDocOwned()
    {
        // Arrange
        var existing = new DocumentModel
        {
            Id = Guid.NewGuid(),
            Title = "Old",
            Summary = "s",
            Tags = "t",
            UploadedAt = DateTimeOffset.UtcNow,
            UserId = _userId
        };

        var dto = new DocumentUpdateDto(
            Id: existing.Id,
            Title: "Updated",
            Summary: "new summary",
            Tags: new List<string> { "tagX" }
        );

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repoMock
            .Setup(r => r.ReadByIdAndUserIdAsync(existing.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.CreateOrUpdateAsync(existing, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var ok = await _service.UpdateAsync(_userId, dto, CancellationToken.None);

        // Assert
        Assert.True(ok);
        Assert.Equal("Updated", existing.Title);

        _repoMock.Verify(r => r.CreateOrUpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenDocOwned()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existing = new DocumentModel
        {
            Id = id,
            Title = "X",
            Summary = "s",
            Tags = "",
            UploadedAt = DateTimeOffset.UtcNow,
            UserId = _userId
        };

        _repoMock
            .Setup(r => r.ReadByIdAndUserIdAsync(id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var ok = await _service.DeleteAsync(_userId, id, CancellationToken.None);

        // Assert
        Assert.True(ok);
        _repoMock.Verify(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
