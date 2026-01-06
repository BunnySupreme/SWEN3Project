using Microsoft.EntityFrameworkCore;
using Paperless.DAL;
using Paperless.DAL.Repositories;
using Xunit;

public class AuthServiceTests
{
    private static DataContext NewDb()
    {
        var opts = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DataContext(opts);
    }

    [Fact]
    public async Task Register_Then_Login_ReturnsToken()
    {
        await using var db = NewDb();
        var users = new UserRepository(db);
        var sessions = new SessionRepository(db);
        var auth = new AuthService(db, users, sessions);

        await auth.RegisterAsync(new("user1", "secret123"), default);
        var res = await auth.LoginAsync(new("user1", "secret123"), default);

        Assert.False(string.IsNullOrWhiteSpace(res.Token));
        Assert.Equal("user1", res.Username);
    }

    [Fact]
    public async Task Login_WrongPassword_Fails()
    {
        await using var db = NewDb();
        var users = new UserRepository(db);
        var sessions = new SessionRepository(db);
        var auth = new AuthService(db, users, sessions);

        await auth.RegisterAsync(new("user1", "secret123"), default);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => auth.LoginAsync(new("user1", "badpw"), default));
    }

    [Fact]
    public async Task Logout_MakesTokenInvalid()
    {
        await using var db = NewDb();
        var users = new UserRepository(db);
        var sessions = new SessionRepository(db);
        var auth = new AuthService(db, users, sessions);

        await auth.RegisterAsync(new("user1", "secret123"), default);
        var res = await auth.LoginAsync(new("user1", "secret123"), default);

        var uBefore = await auth.ValidateTokenAsync(res.Token, default);
        Assert.NotNull(uBefore);

        await auth.LogoutAsync(res.Token, default);

        var uAfter = await auth.ValidateTokenAsync(res.Token, default);
        Assert.Null(uAfter);
    }
}
