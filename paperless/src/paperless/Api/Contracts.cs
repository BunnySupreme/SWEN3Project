namespace Paperless.Api.Contracts;

// Contracts
public sealed record DocumentReadDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt
);

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
