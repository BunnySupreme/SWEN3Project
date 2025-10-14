using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
        var docs = new List<Document>
    {
        new Document(), // older
        new Document()  // newer
    };
        docs[0].Update("File B", "content", "summary", "tag2");
        docs[1].Update("File A", "content", "summary", "tag1");

        _repoMock.Setup(r => r.ReadAll()).Returns(docs);

        var result = await _service.ListAsync(null, 0, 50, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].FileName.Should().Be("File A");
        result[1].FileName.Should().Be("File B");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDto_WhenFound()
    {
        var doc = new Document();
        doc.Update("Some File", "c", "s", "t");
        _repoMock.Setup(r => r.ReadById(doc.Id)).Returns(doc);

        var result = await _service.GetAsync(doc.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.FileName.Should().Be("Some File");
    }

    [Fact]
    public async Task CreateAsync_ShouldCallRepositoryAndReturnDto()
    {
        var createDto = new DocumentCreateDto("NewFile", "application/pdf", "sum", new List<string> { "tag1" });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        _repoMock.Verify(r => r.CreateOrUpdate(It.IsAny<Document>()), Times.Once);
        result.FileName.Should().Be("NewFile");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_WhenDocumentExists()
    {
        var existing = new Document();
        existing.Update("Old", "c", "s", "t");

        _repoMock.Setup(r => r.ReadById(existing.Id)).Returns(existing);

        var dto = new DocumentUpdateDto(existing.Id, "Updated", "application/pdf", "new summary", new List<string> { "tagX" });

        var result = await _service.UpdateAsync(dto, CancellationToken.None);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.CreateOrUpdate(It.Is<Document>(d => d.Title == "Updated")), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallDeleteById()
    {
        var id = Guid.NewGuid();
        var doc = new Document();
        typeof(Document).GetProperty(nameof(Document.Id))!.SetValue(doc, id);

        _repoMock.Setup(r => r.ReadById(id)).Returns(doc);
        _repoMock.Setup(r => r.DeleteById(id));

        var result = await _service.DeleteAsync(id, CancellationToken.None);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteById(id), Times.Once);
    }

}
