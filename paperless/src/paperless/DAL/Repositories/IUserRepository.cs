using Paperless.DAL.Models;

namespace Paperless.DAL.Repositories
{
    public interface IUserRepository
    {
        Task<UserModel?> ReadByIdAsync(Guid id, CancellationToken ct = default);
        Task<UserModel?> ReadByUsernameAsync(string username, CancellationToken ct = default);
        Task CreateAsync(UserModel user, CancellationToken ct = default);
    }
}
