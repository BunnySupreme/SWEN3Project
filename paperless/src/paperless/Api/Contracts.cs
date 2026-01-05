namespace Paperless.Api;

// Contracts
public sealed record DocumentReadDto(
	Guid Id,
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags,
	DateTimeOffset UploadedAt,
    Guid UserId);

public sealed record DocumentCreateDto(
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags,
    Guid UserId);

public sealed record DocumentUpdateDto(
	Guid Id,
	string Title,
	string Summary,
	IReadOnlyList<string>? Tags,
    Guid UserId);

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, DateTimeOffset ExpiresAt);
