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
using Microsoft.AspNetCore.Http;

namespace Paperless.UnitTests
{
    public class DocumentServiceValidationTests
    {
        private readonly Mock<IDocumentRepository> _repoMock;
        private readonly Mock<DataContext> _dbMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IValidator<DocumentCreateDto>> _createValidatorMock;
        private readonly Mock<IValidator<DocumentUpdateDto>> _updateValidatorMock;
        private readonly Mock<IRabbitProducerService> _rabbitProducerMock;
        private readonly Mock<IElasticService> _elasticServiceMock;
        private readonly DocumentService _service;

        public DocumentServiceValidationTests()
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

            _createValidatorMock = new Mock<IValidator<DocumentCreateDto>>();
            _updateValidatorMock = new Mock<IValidator<DocumentUpdateDto>>();

            _rabbitProducerMock = new Mock<IRabbitProducerService>();
            _elasticServiceMock = new Mock<IElasticService>();

            _service = new DocumentService(
                _repoMock.Object, 
                _dbMock.Object, 
                _mapperMock.Object, 
                _createValidatorMock.Object, 
                _updateValidatorMock.Object,
                _rabbitProducerMock.Object,
                _elasticServiceMock.Object);
        }

        [Fact]
        public async Task ListAsync_Valid_ReturnsMappedDtos()
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
        public async Task ListAsync_Faulty_DocHasMoreThan10Tags_ReturnsAllTags()
        {
            // Arrange - repository has a stored document with >10 tags (faulty persisted data)
            var manyTags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var doc = new DocumentModel();
            doc.Update("ManyTags", "s", string.Join(',', manyTags));
            var docs = new List<DocumentModel> { doc };

            _repoMock
                .Setup(r => r.ReadAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(docs);

            // Act
            var result = await _service.ListAsync(null, 0, 50, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].Tags); // <-- Add this null check
            Assert.Equal(11, result[0].Tags!.Count); // <-- Use null-forgiving operator
            Assert.Equal("t0", result[0].Tags![0]);
            Assert.Equal("t10", result[0].Tags![10]);
        }

        [Fact]
        public async Task GetAsync_Valid_ReturnsDto_WhenFound()
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
        public async Task GetAsync_Faulty_DocHasMoreThan10Tags_ReturnsDtoWithManyTags()
        {
            // Arrange
            var manyTags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var doc = new DocumentModel();
            doc.Update("Some File", "s", string.Join(',', manyTags));

            _repoMock
                .Setup(r => r.ReadByIdAsync(doc.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            // Act
            var result = await _service.GetAsync(doc.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.Tags);
            Assert.Equal(11, result.Tags.Count);
        }

        [Fact]
        public async Task CreateAsync_Valid_CreatesAndReturnsDto()
        {
            // Arrange
            var createDto = new DocumentCreateDto(
                Title: "NewFile",
                Summary: "sum",
                Tags: new List<string> { "tag1" });

            // Ensure validator does not throw by returning a successful ValidationResult
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DocumentCreateDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            // Also setup the overload that takes the instance directly (ValidateAsync(T, CancellationToken))
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _repoMock
                .Setup(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()))
                .Returns<DocumentModel, CancellationToken>((doc, ct) =>
                {
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
        public async Task CreateAsync_Invalid_TooManyTags_ThrowsValidationException_AndDoesNotCallRepo()
        {
            // Arrange
            var tags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var createDto = new DocumentCreateDto(Title: "Bad", Summary: "S", Tags: tags);

            // Make validator return an invalid ValidationResult (so ValidateAndThrowAsync will throw)
            var invalidResult = new ValidationResult(new[] { new ValidationFailure("Tags", "A maximum of 10 tags are allowed") });
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DocumentCreateDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidResult);
            // Also setup the overload that takes the instance directly
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidResult);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
            _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UploadAsync_Valid_ReturnsCreatedDto()
        {
            // Arrange
            var content = System.Text.Encoding.UTF8.GetBytes("dummy file content");
            var stream = new MemoryStream(content);
            var formFile = new FormFile(stream, 0, stream.Length, "file", "test.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };
            var dto = new DocumentCreateDto(
                Title: "test.txt",
                Summary: string.Empty,
                Tags: Array.Empty<string>());

            // Ensure validator does not throw
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DocumentCreateDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _repoMock
                .Setup(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()))
                .Returns<DocumentModel, CancellationToken>((doc, ct) =>
                {
                    doc.UploadedAt = DateTimeOffset.UtcNow;
                    return Task.CompletedTask;
                });

            // Act
            var result = await _service.UploadAsync(formFile, dto, CancellationToken.None);

            // Assert
            Assert.Equal("test.txt", result.Title);
            _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadAsync_Invalid_ValidatorThrows_ThrowsValidationException_AndDoesNotCallRepo()
        {
            // Arrange
            var content = System.Text.Encoding.UTF8.GetBytes("dummy file content");
            var stream = new MemoryStream(content);
            var formFile = new FormFile(stream, 0, stream.Length, "file", "test.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };
            var dto = new DocumentCreateDto(
                Title: "test.txt",
                Summary: string.Empty,
                Tags: Array.Empty<string>());

            // Simulate validator detecting invalid data (e.g. more than 10 tags for created DTO)
            var invalidResult = new ValidationResult(new[] { new ValidationFailure("Tags", "A maximum of 10 tags are allowed") });
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DocumentCreateDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidResult);
            _createValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DocumentCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidResult);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.UploadAsync(formFile, dto, CancellationToken.None));
            _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_Valid_ReturnsTrue_WhenDocumentExists()
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

            // Ensure validator does not throw
            _updateValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DocumentUpdateDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            _updateValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DocumentUpdateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

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
        public async Task UpdateAsync_Invalid_TooManyTags_ThrowsValidationException_AndDoesNotCallRepo()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var dto = new DocumentUpdateDto(Id: id, Title: "T", Summary: "S", Tags: tags);

            // Make validator return an invalid ValidationResult (so ValidateAndThrowAsync will throw)
            var invalidUpdateResult = new ValidationResult(new[] { new ValidationFailure("Tags", "A maximum of 10 tags are allowed") });
            _updateValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DocumentUpdateDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidUpdateResult);
            _updateValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<DocumentUpdateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidUpdateResult);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(dto, CancellationToken.None));
            _repoMock.Verify(r => r.ReadByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valid_ReturnsTrue_WhenDeleted()
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

        [Fact]
        public async Task DeleteAsync_Faulty_DocHasMoreThan10Tags_StillDeletes()
        {
            // Arrange - stored doc has >10 tags but delete should still proceed
            var id = Guid.NewGuid();
            var tags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var doc = new DocumentModel { Id = id };
            doc.Update("ToDelete", "s", string.Join(',', tags));

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
}