using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Paperless.Api;
using Paperless.Api.Controllers;
using Paperless.Services;

namespace Paperless.UnitTests;

public class DocumentsControllerTests
{
    private readonly Mock<IDocumentService> _serviceMock;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _serviceMock = new Mock<IDocumentService>();
        _controller = new DocumentsController(_serviceMock.Object);
    }

    [Fact]
    public async Task List_ShouldReturn200AndDocs()
    {
        // Arrange
        var docs = new List<DocumentReadDto>
        {
            new DocumentReadDto(Guid.NewGuid(), "Title1", "Summary1", new[] { "tag1" }, DateTimeOffset.Now),
            new DocumentReadDto(Guid.NewGuid(), "Title2", "Summary2", Array.Empty<string>(), DateTimeOffset.Now)
        };

        _serviceMock
            .Setup(s => s.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        // Act
        var actionResult = await _controller.List(title: null, skip: 0, take: 50);

        // Assert
        actionResult.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)actionResult.Result!;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeAssignableTo<IEnumerable<DocumentReadDto>>();
        ((IEnumerable<DocumentReadDto>)ok.Value!).ToList().Should().BeEquivalentTo(docs, options => options.WithStrictOrdering());

        _serviceMock.Verify(s => s.ListAsync(null, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_ShouldReturn400_ForInvalidParameters()
    {
        // Arrange - invalid skip
        // Act
        var result1 = await _controller.List(title: null, skip: -1, take: 50);

        // Assert
        result1.Result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result1.Result!).StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        // Arrange - invalid take (0)
        var result2 = await _controller.List(title: null, skip: 0, take: 0);
        result2.Result.Should().BeOfType<BadRequestObjectResult>();

        // Arrange - invalid take (exceeds max)
        var result3 = await _controller.List(title: null, skip: 0, take: 1000);
        result3.Result.Should().BeOfType<BadRequestObjectResult>();

        // Service should never be called when parameters are invalid
        _serviceMock.Verify(s => s.ListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_ShouldReturn200_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new DocumentReadDto(id, "T", "S", Array.Empty<string>(), DateTimeOffset.Now);
        _serviceMock.Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.Get(id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result.Result!;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Get_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((DocumentReadDto?)null);

        // Act
        var result = await _controller.Get(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        ((NotFoundResult)result.Result!).StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenCreated()
    {
        // Arrange
        var createDto = new DocumentCreateDto(Title: "New", Summary: "S", Tags: Array.Empty<string>());
        var created = new DocumentReadDto(Guid.NewGuid(), "New", "S", Array.Empty<string>(), DateTimeOffset.Now);

        _serviceMock
            .Setup(s => s.CreateAsync(It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdAt = (CreatedAtActionResult)result.Result!;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.ActionName.Should().Be(nameof(DocumentsController.Get));
        createdAt.Value.Should().BeEquivalentTo(created);

        _serviceMock.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Note: 400 responses for Create are produced by the framework's automatic model validation (ApiController).
    // That behavior runs as a filter in the MVC pipeline and is not executed when calling the action method directly in a unit test.
    // For the purpose of unit testing controller logic we assert the successful creation path above.

    [Fact]
    public async Task Upload_ShouldReturn201_WhenFileProvided()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("dummy file content");
        var stream = new MemoryStream(content);
        var formFile = new FormFile(stream, 0, stream.Length, "file", "test.pdf") // Changed to .pdf
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf" // Changed to application/pdf
        };

        var created = new DocumentReadDto(Guid.NewGuid(), "test.pdf", string.Empty, Array.Empty<string>(), DateTimeOffset.Now);

        _serviceMock
            .Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Upload(formFile, title: null, tags: null);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdAt = (CreatedAtActionResult)result.Result!;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.Value.Should().BeEquivalentTo(created);

        _serviceMock.Verify(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturn204_WhenUpdated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new DocumentUpdateDto(Id: id, Title: "T", Summary: "S", Tags: Array.Empty<string>());

        _serviceMock.Setup(s => s.UpdateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.Update(id, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        ((NoContentResult)result).StatusCode.Should().Be(StatusCodes.Status204NoContent);
        _serviceMock.Verify(s => s.UpdateAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenIdMismatch()
    {
        // Arrange
        var dto = new DocumentUpdateDto(Id: Guid.NewGuid(), Title: "T", Summary: "S", Tags: Array.Empty<string>());
        var routeId = Guid.NewGuid(); // different

        // Act
        var result = await _controller.Update(routeId, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        // Ensure service was not called
        _serviceMock.Verify(s => s.UpdateAsync(It.IsAny<DocumentUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new DocumentUpdateDto(Id: id, Title: "T", Summary: "S", Tags: Array.Empty<string>());

        _serviceMock.Setup(s => s.UpdateAsync(dto, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.Update(id, dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        ((NotFoundResult)result).StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        ((NoContentResult)result).StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        ((NotFoundResult)result).StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}

