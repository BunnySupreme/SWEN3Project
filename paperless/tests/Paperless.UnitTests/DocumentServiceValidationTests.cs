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

namespace Paperless.UnitTests
{
    public class DocumentServiceValidationTests
    {
        private readonly Mock<IDocumentRepository> _repoMock = new();
        private readonly DataContext _db;
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IValidator<DocumentCreateDto>> _createValidatorMock = new();
        private readonly Mock<IValidator<DocumentUpdateDto>> _updateValidatorMock = new();
        private readonly Mock<IRabbitProducerService> _rabbitProducerMock = new();

        private readonly DocumentService _service;

        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _otherUserId = Guid.NewGuid();

        public DocumentServiceValidationTests()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new DataContext(options);

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
                .Returns((DocumentCreateDto dto) => new DocumentModel
                {
                    Title = dto.Title,
                    Summary = dto.Summary ?? string.Empty,
                    Tags = dto.Tags is null ? string.Empty : string.Join(',', dto.Tags),
                    UploadedAt = DateTimeOffset.UtcNow
                });

            _service = new DocumentService(
                _repoMock.Object,
                _db,
                _mapperMock.Object,
                _createValidatorMock.Object,
                _updateValidatorMock.Object,
                _rabbitProducerMock.Object);
        }

        [Fact]
        public async Task ListAsync_Valid_CallsRepoWithUserId_AndMapsResults()
        {
            // Arrange
            var docsForUser = new List<DocumentModel>
            {
                new DocumentModel { Id = Guid.NewGuid(), Title = "Mine A", Summary = "s", Tags = "tag1", UploadedAt = DateTimeOffset.UtcNow, UserId = _userId },
                new DocumentModel { Id = Guid.NewGuid(), Title = "Mine B", Summary = "s", Tags = "tag2", UploadedAt = DateTimeOffset.UtcNow.AddMinutes(-1), UserId = _userId }
            };

            _repoMock
                .Setup(r => r.ReadListAsync(_userId, null, 0, 50, It.IsAny<CancellationToken>()))
                .ReturnsAsync(docsForUser);

            // Act
            var result = await _service.ListAsync(_userId, title: null, skip: 0, take: 50, ct: CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);

            _repoMock.Verify(r => r.ReadListAsync(_userId, null, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ListAsync_Faulty_DocHasMoreThan10Tags_ReturnsAllTags()
        {
            // Arrange
            var manyTags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var doc = new DocumentModel
            {
                Id = Guid.NewGuid(),
                Title = "ManyTags",
                Summary = "s",
                Tags = string.Join(',', manyTags),
                UploadedAt = DateTimeOffset.UtcNow,
                UserId = _userId
            };

            _repoMock
                .Setup(r => r.ReadListAsync(_userId, null, 0, 50, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DocumentModel> { doc });

            // Act
            var result = await _service.ListAsync(_userId, title: null, skip: 0, take: 50, ct: CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal(11, result[0].Tags.Count);
            Assert.Equal("t0", result[0].Tags[0]);
            Assert.Equal("t10", result[0].Tags[10]);
        }

        [Fact]
        public async Task GetAsync_Valid_ReturnsDto_WhenFound_AndOwned()
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
        public async Task GetAsync_Faulty_DocHasMoreThan10Tags_ReturnsDtoWithManyTags_WhenOwned()
        {
            // Arrange
            var manyTags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var doc = new DocumentModel
            {
                Id = Guid.NewGuid(),
                Title = "Some File",
                Summary = "s",
                Tags = string.Join(',', manyTags),
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
            Assert.Equal(11, result!.Tags.Count);
        }

        [Fact]
        public async Task CreateAsync_Valid_CreatesAndReturnsDto_WithUserIdSetByService()
        {
            // Arrange
            var createDto = new DocumentCreateDto(
                Title: "NewFile",
                Summary: "sum",
                Tags: new List<string> { "tag1" }
            );

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _repoMock
                .Setup(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(_userId, createDto, CancellationToken.None);

            // Assert
            Assert.Equal("NewFile", result.Title);

            _repoMock.Verify(r => r.CreateOrUpdateAsync(
                It.Is<DocumentModel>(m => m.UserId == _userId && m.Title == "NewFile"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Invalid_TooManyTags_ThrowsValidationException_AndDoesNotCallRepo()
        {
            // Arrange
            var tags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var createDto = new DocumentCreateDto(
                Title: "Bad",
                Summary: "S",
                Tags: tags
            );

            var invalidResult = new ValidationResult(new[]
            {
                new ValidationFailure("Tags", "A maximum of 10 tags are allowed")
            });

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidResult);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(_userId, createDto, CancellationToken.None));
            _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_Valid_ReturnsTrue_WhenDocumentExists_AndOwned()
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
        public async Task UpdateAsync_Invalid_TooManyTags_ThrowsValidationException_AndDoesNotCallRepo()
        {
            // Arrange
            var tags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var dto = new DocumentUpdateDto(
                Id: Guid.NewGuid(),
                Title: "T",
                Summary: "S",
                Tags: tags
            );

            var invalidResult = new ValidationResult(new[]
            {
                new ValidationFailure("Tags", "A maximum of 10 tags are allowed")
            });

            _updateValidatorMock
                .Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidResult);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(_userId, dto, CancellationToken.None));

            // Because the validator fails first, service must not query repo at all.
            _repoMock.Verify(r => r.ReadByIdAndUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.Verify(r => r.CreateOrUpdateAsync(It.IsAny<DocumentModel>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Valid_ReturnsTrue_WhenDeleted_AndOwned()
        {
            // Arrange
            var id = Guid.NewGuid();
            var doc = new DocumentModel
            {
                Id = id,
                Title = "ToDelete",
                Summary = "s",
                Tags = "",
                UploadedAt = DateTimeOffset.UtcNow,
                UserId = _userId
            };

            _repoMock
                .Setup(r => r.ReadByIdAndUserIdAsync(id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

            _repoMock
                .Setup(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var ok = await _service.DeleteAsync(_userId, id, CancellationToken.None);

            // Assert
            Assert.True(ok);
            _repoMock.Verify(r => r.DeleteByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Faulty_DocHasMoreThan10Tags_StillDeletes_WhenOwned()
        {
            // Arrange
            var id = Guid.NewGuid();
            var tags = Enumerable.Range(0, 11).Select(i => $"t{i}").ToArray();
            var doc = new DocumentModel
            {
                Id = id,
                Title = "ToDelete",
                Summary = "s",
                Tags = string.Join(',', tags),
                UploadedAt = DateTimeOffset.UtcNow,
                UserId = _userId
            };

            _repoMock
                .Setup(r => r.ReadByIdAndUserIdAsync(id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(doc);

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
}
