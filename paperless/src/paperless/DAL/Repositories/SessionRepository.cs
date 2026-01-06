using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Models;

namespace Paperless.DAL.Repositories;

public sealed class SessionRepository : ISessionRepository
{
    private readonly DataContext _db;
    public SessionRepository(DataContext db) => _db = db;

    public Task<SessionModel?> ReadByTokenAsync(string token, CancellationToken ct = default) =>
        _db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Token == token, ct);

    public Task AddAsync(SessionModel session, CancellationToken ct = default)
    {
        _db.Sessions.Add(session);
        return Task.CompletedTask;
    }

    public async Task<bool> RevokeAsync(string token, DateTimeOffset revokedAtUtc, CancellationToken ct = default)
    {
        var s = await _db.Sessions.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (s is null) return false;
        s.RevokedAt = revokedAtUtc;
        _db.Sessions.Remove(s);
        return true;
    }
}
