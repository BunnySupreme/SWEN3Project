using Microsoft.EntityFrameworkCore;
using Paperless.DAL.Models;

namespace Paperless.DAL.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        #region DataContext
        private readonly DataContext _db;
        #endregion

        #region Constructors
        public UserRepository(DataContext db) => _db = db;
        #endregion

        public Task<UserModel?> ReadByIdAsync(Guid id, CancellationToken ct = default) =>
            _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<UserModel?> ReadByUsernameAsync(string username, CancellationToken ct = default) =>
            _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct);

        public Task CreateAsync(UserModel user, CancellationToken ct = default) =>
            _db.Users.AddAsync(user, ct).AsTask();
    }
}
