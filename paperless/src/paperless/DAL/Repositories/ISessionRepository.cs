using Paperless.DAL.Models;

namespace Paperless.DAL.Repositories;

public interface ISessionRepository
{
    Task<SessionModel?> ReadByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(SessionModel session, CancellationToken ct = default);
    Task<bool> RevokeAsync(string token, DateTimeOffset revokedAtUtc, CancellationToken ct = default);
}
