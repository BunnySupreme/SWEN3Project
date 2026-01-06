using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        await _auth.RegisterAsync(req, ct);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
        => Ok(await _auth.LoginAsync(req, ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = ReadBearerToken(Request);
        if (token is null) return Unauthorized();
        await _auth.LogoutAsync(token, ct);
        return NoContent();
    }

    private static string? ReadBearerToken(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("Authorization", out var h)) return null;
        var s = h.ToString();
        return s.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? s["Bearer ".Length..].Trim()
            : null;
    }
}
