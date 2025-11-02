using AutoMapper;
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
    private readonly Mock<IDocumentRepository> _repoMock;
    private readonly Mock<DataContext> _dbMock; 
    private readonly Mock<IMapper> _mapperMock;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbMock = new Mock<DataContext>(options) { CallBase = true };
        _dbMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repoMock = new Mock<IDocumentRepository>();

        _mapperMock = new Mock<IMapper>();
        _mapperMock
            .Setup(m => m.Map<DocumentReadDto>(It.IsAny<DocumentModel>()))
            .Returns((DocumentModel d) => new DocumentReadDto(
                Id: d.Id,
                Title: d.Title,
                Summary: d.Summary,
                Tags: string.IsNullOrWhiteSpace(d.Tags)
                    ? Array.Empty<string>()
                    : d.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                UploadedAt: d.UploadedAt
            ));
        _mapperMock
            .Setup(m => m.Map<DocumentModel>(It.IsAny<DocumentCreateDto>()))
            .Returns((DocumentCreateDto dto) =>
            {
                var model = new DocumentModel();
                model.Update(dto.Title, dto.Summary, dto.Tags is null ? string.Empty : string.Join(',', dto.Tags));
                return model;
            });

        _service = new DocumentService(_repoMock.Object, _dbMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnMappedDtos()
    {
        // Arrange
        var docs = new List<DocumentModel>
        {
            new DocumentModel(), // older
            new DocumentModel()  // newer
        };
        docs[0].Update("File B", "summary", "tag2");
        docs[1].Update("File A", "summary", "tag1");

        _repoMock
            .Setup(r => r.ReadAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        // Act
        var result = await _service.ListAsync(null, 0, 50, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("File A", result[0].Title);
        Assert.Equal("File B", result[1].Title);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDto_WhenFound()
    {
        // Arrange
        var doc = new DocumentModel();
        doc.Update("Some File", "s", "t");

        _repoMock
            .Setup(r => r.ReadByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        // Act
        var result = await _service.GetAsync(doc.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Some File", result!.Title);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallRepositoryAndReturnDto()
    {
        // Arrange
        var createDto = new DocumentCreateDto(
            Title: "NewFile",
            Summary: "sum",
            Tags: new List<string> { "tag1" });

        _repoMock
            .Setup(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()))
            .Returns<DocumentModel, CancellationToken>((doc, ct) =>
            {
                // simulate repository/db setting persistence fields
                doc.UploadedAt = DateTimeOffset.UtcNow;
                return Task.CompletedTask;
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("NewFile", result.Title);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_WhenDocumentExists()
    {
        // Arrange
        var existing = new DocumentModel();
        existing.Update("Old", "s", "t");

        _repoMock
            .Setup(r => r.ReadByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new DocumentUpdateDto(
            Id: existing.Id,
            Title: "Updated",
            Summary: "new summary",
            Tags: new List<string> { "tagX" });

        // Act
        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        // Assert
        Assert.True(result);
        _repoMock.Verify(
            r => r.CreateOrUpdateAsync(
                It.Is<DocumentModel>(d => d.Title == "Updated"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallDeleteById()
    {
        // Arrange
        var id = Guid.NewGuid();
        var doc = new DocumentModel { Id = id };

        _repoMock
            .Setup(r => r.ReadByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        _repoMock
            .Setup(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.True(result);
        _repoMock.Verify(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}