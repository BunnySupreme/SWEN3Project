namespace Paperless.Api.Contracts;

public sealed record DocumentReadDto(
	Guid Id,
	string FileName,
	string ContentType,
	DateTimeOffset UploadedAt,
	string? Summary,
	IReadOnlyList<string> Tags);

public sealed record DocumentCreateDto(
	string FileName,
	string? ContentType,
	string? Summary,
	IReadOnlyList<string> Tags);

public sealed record DocumentUpdateDto(
	Guid Id,
	string FileName,
	string? ContentType,
	string? Summary,
	IReadOnlyList<string> Tags);
