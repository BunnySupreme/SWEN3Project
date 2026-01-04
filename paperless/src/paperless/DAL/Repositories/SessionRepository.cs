using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Models;

namespace Paperless.DAL.Repositories
{
    public sealed class SessionRepository : ISessionRepository
    {
        private readonly DataContext _db;

        public SessionRepository(DataContext db) => _db = db;

        public Task<SessionModel?> ReadByTokenAsync(string token, CancellationToken ct = default) =>
            _db.Sessions.FirstOrDefaultAsync(s => s.Token == token, ct);

        public Task CreateAsync(SessionModel session, CancellationToken ct = default) =>
            _db.Sessions.AddAsync(session, ct).AsTask();

        public async Task<bool> RevokeAsync(string token, CancellationToken ct = default)
        {
            var s = await _db.Sessions.FirstOrDefaultAsync(x => x.Token == token, ct);
            if (s is null) return false;
            if (s.RevokedAt != null) return true;

            s.Revoke(DateTimeOffset.UtcNow);
            return true;
        }
    }
}
