using Paperless.DAL.Models;
using Paperless.DAL.Repositories;
using Paperless.DAL;
using Microsoft.EntityFrameworkCore;

public sealed class UserRepository : IUserRepository
{
    private readonly DataContext _db;
    public UserRepository(DataContext db) => _db = db;

    public Task<UserModel?> ReadByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<UserModel?> ReadByUsernameAsync(string username, CancellationToken ct = default)
    {
        var norm = Normalize(username);
        return _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToUpper() == norm, ct);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        var norm = Normalize(username);
        return _db.Users.AsNoTracking()
            .AnyAsync(u => u.Username.ToUpper() == norm, ct);
    }

    public Task AddAsync(UserModel user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        return Task.CompletedTask;
    }

    private static string Normalize(string s) => s.Trim().ToUpperInvariant();
}