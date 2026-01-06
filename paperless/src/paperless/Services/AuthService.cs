using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Paperless.Api.Controllers;
using Paperless.DAL;
using Paperless.DAL.Models;
using Paperless.DAL.Repositories;

public sealed class AuthService : IAuthService
{
    private readonly DataContext _db;
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly PasswordHasher<UserModel> _hasher = new();

    public AuthService(DataContext db, IUserRepository users, ISessionRepository sessions)
    {
        _db = db;
        _users = users;
        _sessions = sessions;
    }

    public async Task RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var username = (req.Username ?? string.Empty).Trim();

        if (await _users.UsernameExistsAsync(username, ct))
            throw new InvalidOperationException("Username already exists.");

        var user = new UserModel { Username = username };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        await _users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var username = (req.Username ?? string.Empty).Trim();
        var user = await _users.ReadByUsernameAsync(username, ct);
        if (user is null) throw new InvalidOperationException("Invalid credentials.");

        var vr = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? string.Empty);
        if (vr == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid credentials.");

        var token = CreateToken();
        var expires = DateTimeOffset.UtcNow.AddDays(1);

        var session = new SessionModel
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = expires,
            RevokedAt = null
        };

        await _sessions.AddAsync(session, ct);
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(token, expires, user.Id, user.Username);
    }

    public async Task<UserModel?> ValidateTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var s = await _sessions.ReadByTokenAsync(token, ct);
        if (s is null) return null;
        if (s.RevokedAt is not null) return null;
        if (s.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await _sessions.RevokeAsync(token, DateTimeOffset.UtcNow, ct);
            return null;
        }
            

        return await _users.ReadByIdAsync(s.UserId, ct);
    }

    public async Task LogoutAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        var ok = await _sessions.RevokeAsync(token, DateTimeOffset.UtcNow, ct);
        if (ok) await _db.SaveChangesAsync(ct);
    }

    private static string CreateToken()
    {
        // 32 bytes => 256-bit
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}


public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);

public record AuthResponse(string Token, DateTimeOffset ExpiresAt, Guid UserId, string Username);
