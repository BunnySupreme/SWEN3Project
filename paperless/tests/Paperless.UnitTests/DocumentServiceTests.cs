using FluentAssertions;
using Moq;
using Paperless.Api;
using Paperless.Services;
using Paperless.DAL.Models;
using Paperless.DAL.Repositories;
using AutoMapper;

namespace Paperless.UnitTests;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _repoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly DocumentService _service;

    public DocumentServiceTests()
    {
        _repoMock = new Mock<IDocumentRepository>();
        _service = new DocumentService(_repoMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnMappedDtos()
    {
        var docs = new List<DocumentModel>
    {
        new DocumentModel(), // older
        new DocumentModel()  // newer
    };
        docs[0].Update("File B", "content", "summary", "tag2");
        docs[1].Update("File A", "content", "summary", "tag1");

        _repoMock.Setup(r => r.ReadAll()).Returns(docs);

        var result = await _service.ListAsync(null, 0, 50, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("File A");
        result[1].Title.Should().Be("File B");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDto_WhenFound()
    {
        var doc = new DocumentModel();
        doc.Update("Some File", "c", "s", "t");
        _repoMock.Setup(r => r.ReadById(doc.Id)).Returns(doc);

        var result = await _service.GetAsync(doc.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Some File");
    }

    [Fact]
    public async Task CreateAsync_ShouldCallRepositoryAndReturnDto()
    {
        var createDto = new DocumentCreateDto("NewFile", "application/pdf", "sum", new List<string> { "tag1" });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        _repoMock.Verify(r => r.CreateOrUpdate(It.IsAny<DocumentModel>()), Times.Once);
        result.Title.Should().Be("NewFile");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_WhenDocumentExists()
    {
        var existing = new DocumentModel();
        existing.Update("Old", "c", "s", "t");

        _repoMock.Setup(r => r.ReadById(existing.Id)).Returns(existing);

        var dto = new DocumentUpdateDto(existing.Id, "Updated", "application/pdf", "new summary", new List<string> { "tagX" });

        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.CreateOrUpdate(It.Is<DocumentModel>(d => d.Title == "Updated")), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallDeleteById()
    {
        var id = Guid.NewGuid();
        var doc = new DocumentModel();
        typeof(DocumentModel).GetProperty(nameof(DocumentModel.Id))!.SetValue(doc, id);

        _repoMock.Setup(r => r.ReadById(id)).Returns(doc);
        _repoMock.Setup(r => r.DeleteById(id));

        var result = await _service.DeleteAsync(id, CancellationToken.None);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteById(id), Times.Once);
    }

}
