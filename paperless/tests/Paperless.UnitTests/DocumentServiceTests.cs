using Moq;
using Paperless.Api.Contracts;
using Paperless.Services;
using paperless.DAL.Models;
using paperless.DAL.Repositories;
using Xunit;

namespace Paperless.UnitTests;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _repoMock;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _repoMock = new Mock<IDocumentRepository>();
        _service = new DocumentService(_repoMock.Object);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnMappedDtos()
    {
        // Arrange
        var docs = new List<Document>
        {
            new Document(), // older
            new Document()  // newer
        };
        docs[0].Update("File B", "content", "summary", "tag2");
        docs[1].Update("File A", "content", "summary", "tag1");

        _repoMock
            .Setup(r => r.ReadAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        // Act
        var result = await _service.ListAsync(null, 0, 50, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("File A", result[0].FileName);
        Assert.Equal("File B", result[1].FileName);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDto_WhenFound()
    {
        // Arrange
        var doc = new Document();
        doc.Update("Some File", "c", "s", "t");

        _repoMock
            .Setup(r => r.ReadByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        // Act
        var result = await _service.GetAsync(doc.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Some File", result!.FileName);
    }

    [Fact]
    public async Task CreateAsync_ShouldCallRepositoryAndReturnDto()
    {
        // Arrange
        var createDto = new DocumentCreateDto(
            FileName: "NewFile",
            ContentType: "application/pdf",
            Summary: "sum",
            Tags: new List<string> { "tag1" });

        _repoMock
            .Setup(r => r.CreateOrUpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("NewFile", result.FileName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_WhenDocumentExists()
    {
        // Arrange
        var existing = new Document();
        existing.Update("Old", "c", "s", "t");

        _repoMock
            .Setup(r => r.ReadByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _repoMock
            .Setup(r => r.CreateOrUpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new DocumentUpdateDto(
            Id: existing.Id,
            FileName: "Updated",
            ContentType: "application/pdf",
            Summary: "new summary",
            Tags: new List<string> { "tagX" });

        // Act
        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        // Assert
        Assert.True(result);
        _repoMock.Verify(
            r => r.CreateOrUpdateAsync(
                It.Is<Document>(d => d.Title == "Updated"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallDeleteById()
    {
        var id = Guid.NewGuid();
        var doc = new Document { Id = id };

        _repoMock
            .Setup(r => r.ReadByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        _repoMock
            .Setup(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync(id, CancellationToken.None);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
