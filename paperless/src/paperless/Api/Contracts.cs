namespace Paperless.Api;

// Contracts
public sealed record DocumentReadDto(
	Guid Id,
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags,
	DateTimeOffset UploadedAt,
	int AccessCount);

public sealed record DocumentCreateDto(
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags,
	int AccessCount);

public sealed record DocumentUpdateDto(
	Guid Id,
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags,
	int AccessCount);
