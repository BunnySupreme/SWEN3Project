using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Paperless.Api;
using Paperless.Api.Controllers;
using Paperless.DAL.Models;
using Paperless.Services;

namespace Paperless.UnitTests;

public class DocumentsControllerTests
{
    private const string TestToken = "test-token";
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IDocumentService> _documentServiceMock;
    private readonly Mock<IElasticService> _elasticServiceMock;
    private readonly Mock<IAuthService> _authMock;
    private readonly DocumentsController _controller;


    public DocumentsControllerTests()
    {
        _documentServiceMock = new Mock<IDocumentService>();
        _elasticServiceMock = new Mock<IElasticService>();
		_authMock = new Mock<IAuthService>();

        _controller = new DocumentsController(_documentServiceMock.Object, _elasticServiceMock.Object, _authMock.Object);

        SetBearer(_controller, TestToken);

        _authMock
            .Setup(a => a.ValidateTokenAsync(TestToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserModel { Id = TestUserId }); 
    }

    private static void SetBearer(ControllerBase controller, string token)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Authorization = $"Bearer {token}";

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = http
        };
    }


    [Fact]
    public async Task List_ShouldReturn200AndDocs()
    {
        // Arrange
        var docs = new List<DocumentReadDto>
        {
            new DocumentReadDto(Guid.NewGuid(), "Title1", "Summary1", new[] { "tag1" }, DateTimeOffset.Now, AccessCount : 0),
            new DocumentReadDto(Guid.NewGuid(), "Title2", "Summary2", Array.Empty<string>(), DateTimeOffset.Now, AccessCount : 0)
        };

        _documentServiceMock
            .Setup(s => s.ListAsync(TestUserId, It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);

        // Act
        var actionResult = await _controller.List(title: null, skip: 0, take: 50);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)actionResult!;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeAssignableTo<IEnumerable<DocumentReadDto>>();

        ((IEnumerable<DocumentReadDto>)ok.Value!).ToList()
            .Should().BeEquivalentTo(docs, options => options.WithStrictOrdering());

        _documentServiceMock.Verify(s => s.ListAsync(TestUserId, null, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_ShouldReturn400_ForInvalidParameters()
    {
        // Act
        var result1 = await _controller.List(title: null, skip: -1, take: 50);
        var result2 = await _controller.List(title: null, skip: 0, take: 0);
        var result3 = await _controller.List(title: null, skip: 0, take: 1000);

        // Assert
        result1.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result1!).StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        result2.Should().BeOfType<BadRequestObjectResult>();
        result3.Should().BeOfType<BadRequestObjectResult>();

        _documentServiceMock.Verify(
            s => s.ListAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_ShouldReturn200_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new DocumentReadDto(id, "T", "S", Array.Empty<string>(), DateTimeOffset.Now, AccessCount: 0);

        _documentServiceMock
            .Setup(s => s.GetAsync(TestUserId, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.Get(id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result!;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(dto);

        _documentServiceMock.Verify(s => s.GetAsync(TestUserId, id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentServiceMock
            .Setup(s => s.GetAsync(TestUserId, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentReadDto?)null);

        // Act
        var result = await _controller.Get(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        ((NotFoundResult)result!).StatusCode.Should().Be(StatusCodes.Status404NotFound);

        _documentServiceMock.Verify(s => s.GetAsync(TestUserId, id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Download_ShouldReturn200_WhenFileExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var fileContent = Encoding.UTF8.GetBytes("PDF file content");
        var memoryStream = new MemoryStream(fileContent);

        _documentServiceMock
            .Setup(s => s.DownloadAsync(TestUserId, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemoryStream?)memoryStream);

        // Act
        var result = await _controller.Download(id);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = (FileStreamResult)result!;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be($"{id}.pdf");

        _documentServiceMock.Verify(s => s.DownloadAsync(TestUserId, id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Download_ShouldReturn404_WhenFileNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _documentServiceMock
            .Setup(s => s.DownloadAsync(TestUserId, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MemoryStream?)null);

        // Act
        var result = await _controller.Download(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        ((NotFoundResult)result!).StatusCode.Should().Be(StatusCodes.Status404NotFound);

        _documentServiceMock.Verify(s => s.DownloadAsync(TestUserId, id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenCreated()
    {
        // Arrange
        var createDto = new DocumentCreateDto(Title: "New", Summary: "S", Tags: Array.Empty<string>(), AccessCount: 0);
        var created = new DocumentReadDto(Guid.NewGuid(), "New", "S", Array.Empty<string>(), DateTimeOffset.Now, AccessCount: 0);

        _documentServiceMock
            .Setup(s => s.CreateAsync(TestUserId, It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdAt = (CreatedAtActionResult)result!;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.ActionName.Should().Be(nameof(DocumentsController.Get));
        createdAt.Value.Should().BeEquivalentTo(created);

        _documentServiceMock.Verify(s => s.CreateAsync(TestUserId, createDto, It.IsAny<CancellationToken>()), Times.Once);
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

        var formFile = new FormFile(stream, 0, stream.Length, "file", "test.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var created = new DocumentReadDto(Guid.NewGuid(), "test.pdf", string.Empty, Array.Empty<string>(), DateTimeOffset.Now, AccessCount: 0);

        _documentServiceMock
            .Setup(s => s.UploadAsync(TestUserId, It.IsAny<IFormFile>(), It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Upload(formFile, title: null, tags: null);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdAt = (CreatedAtActionResult)result!;
        createdAt.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdAt.Value.Should().BeEquivalentTo(created);

        _documentServiceMock.Verify(
            s => s.UploadAsync(TestUserId, It.IsAny<IFormFile>(), It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturn204_WhenUpdated()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new DocumentUpdateDto(Id: id, Title: "T", Summary: "S", Tags: Array.Empty<string>(), AccessCount: 0);

        _documentServiceMock
            .Setup(s => s.UpdateAsync(TestUserId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Update(id, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        ((NoContentResult)result).StatusCode.Should().Be(StatusCodes.Status204NoContent);

        _documentServiceMock.Verify(s => s.UpdateAsync(TestUserId, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenIdMismatch()
    {
        // Arrange
        var dto = new DocumentUpdateDto(Id: Guid.NewGuid(), Title: "T", Summary: "S", Tags: Array.Empty<string>(), AccessCount: 0);
        var routeId = Guid.NewGuid();

        // Act
        var result = await _controller.Update(routeId, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        _documentServiceMock.Verify(
            s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<DocumentUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new DocumentUpdateDto(Id: id, Title: "T", Summary: "S", Tags: Array.Empty<string>(), AccessCount: 0);

        _documentServiceMock
            .Setup(s => s.UpdateAsync(TestUserId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Update(id, dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        ((NotFoundResult)result).StatusCode.Should().Be(StatusCodes.Status404NotFound);

        _documentServiceMock.Verify(s => s.UpdateAsync(TestUserId, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturn204_WhenDeleted()
    {
        // Arrange
        var id = Guid.NewGuid();

        _documentServiceMock
            .Setup(s => s.DeleteAsync(TestUserId, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        ((NoContentResult)result).StatusCode.Should().Be(StatusCodes.Status204NoContent);

        _documentServiceMock.Verify(s => s.DeleteAsync(TestUserId, id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _documentServiceMock
            .Setup(s => s.DeleteAsync(TestUserId, id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        ((NotFoundResult)result!).StatusCode.Should().Be(StatusCodes.Status404NotFound);

        _documentServiceMock.Verify(s => s.DeleteAsync(TestUserId, id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
