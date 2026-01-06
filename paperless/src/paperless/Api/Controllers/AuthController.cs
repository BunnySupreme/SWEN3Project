using Microsoft.AspNetCore.Mvc;
using Paperless.Services;

namespace Paperless.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        await auth.RegisterAsync(req.Username, req.Password, ct);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
        => Ok(await auth.LoginAsync(req.Username, req.Password, ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = ReadBearerToken(Request);
        if (token is null) return Unauthorized();
        await auth.LogoutAsync(token, ct);
        return NoContent();
    }

    private static string? ReadBearerToken(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("Authorization", out var h)) return null;
        var s = h.ToString();
        return s.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? s["Bearer ".Length..].Trim() : null;
    }
}

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, DateTimeOffset ExpiresAt);
