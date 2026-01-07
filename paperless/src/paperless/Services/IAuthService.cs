using Paperless.Api;
using Paperless.Api.Controllers;
using Paperless.DAL.Models;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<UserModel?> ValidateTokenAsync(string token, CancellationToken ct);
    Task LogoutAsync(string token, CancellationToken ct);
}
