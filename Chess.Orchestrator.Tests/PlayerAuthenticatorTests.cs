using Chess.Orchestrator.Tests.Support;

namespace Chess.Orchestrator.Tests;

public class PlayerAuthenticatorTests
{
    private static (PlayerAuthenticator Authenticator, MockPlayerRepository Repository) CreateAuthenticator()
    {
        var repository = new MockPlayerRepository();
        var authenticator = new PlayerAuthenticator(repository.Object);
        return (authenticator, repository);
    }

    [Fact]
    public async Task RegisterAsync_ValidCredentials_PersistsHashedPasswordAndReturnsPlayer()
    {
        var (authenticator, repo) = CreateAuthenticator();

        var result = await authenticator.RegisterAsync("magnus", "hunter2pass");

        Assert.Equal("magnus", result.Username);

        var added = Assert.Single(repo.AddedPlayers);
        Assert.Equal(result.Id, added.Id);
        Assert.NotEqual("hunter2pass", added.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("hunter2pass", added.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_UsernameAlreadyTaken_ThrowsUsernameTakenException()
    {
        var (authenticator, _) = CreateAuthenticator();
        await authenticator.RegisterAsync("magnus", "hunter2pass");

        await Assert.ThrowsAsync<UsernameTakenException>(() => authenticator.RegisterAsync("magnus", "otherpassword"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("this-username-is-way-too-long-to-be-valid")]
    public async Task RegisterAsync_UsernameWrongLength_ThrowsInvalidUsernameException(string username)
    {
        var (authenticator, _) = CreateAuthenticator();

        await Assert.ThrowsAsync<InvalidUsernameException>(() => authenticator.RegisterAsync(username, "hunter2pass"));
    }

    [Fact]
    public async Task RegisterAsync_ShortPassword_ThrowsWeakPasswordException()
    {
        var (authenticator, _) = CreateAuthenticator();

        await Assert.ThrowsAsync<WeakPasswordException>(() => authenticator.RegisterAsync("magnus", "short"));
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsPlayer()
    {
        var (authenticator, _) = CreateAuthenticator();
        var registered = await authenticator.RegisterAsync("magnus", "hunter2pass");

        var result = await authenticator.LoginAsync("magnus", "hunter2pass");

        Assert.Equal(registered.Id, result.Id);
        Assert.Equal("magnus", result.Username);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var (authenticator, _) = CreateAuthenticator();
        await authenticator.RegisterAsync("magnus", "hunter2pass");

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => authenticator.LoginAsync("magnus", "wrongpassword"));
    }

    [Fact]
    public async Task LoginAsync_UnknownUsername_ThrowsInvalidCredentialsException()
    {
        var (authenticator, _) = CreateAuthenticator();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => authenticator.LoginAsync("ghost", "hunter2pass"));
    }
}
