namespace Paperless.Api;

public sealed record DocumentReadDto(
	Guid Id,
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags,
	DateTimeOffset UploadedAt);

public sealed record DocumentCreateDto(
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags);

public sealed record DocumentUpdateDto(
	Guid Id,
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags);
