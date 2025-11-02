using System.ComponentModel.DataAnnotations;

namespace Paperless.Api;

public sealed record DocumentReadDto(
	[Required] Guid Id,
	[Required][StringLength(255)] string Title,
	[Required] string Content,
	[Required] string Summary,
	IReadOnlyList<string>? Tags,
	[Required] DateTimeOffset UploadedAt);

public sealed record DocumentCreateDto(
	[Required][StringLength(255)] string Title,
	[Required] string Content,
	[Required] string Summary,
	IReadOnlyList<string>? Tags);

public sealed record DocumentUpdateDto(
	[Required] Guid Id,
	[Required][StringLength(255)] string Title,
	[Required] string Content,
	[Required] string Summary,
	IReadOnlyList<string>? Tags);
