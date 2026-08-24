using Chess.Orchestrator.Tests.Support;

namespace Chess.Orchestrator.Tests;

public class PlayerOrchestratorTests
{
    private static (PlayerOrchestrator Orchestrator, MockPlayerRepository Repository) CreatePlayerOrchestrator()
    {
        var repository = new MockPlayerRepository();
        var orchestrator = new PlayerOrchestrator(repository.Object);
        return (orchestrator, repository);
    }

    [Fact]
    public async Task RegisterAsync_ValidCredentials_PersistsHashedPasswordAndReturnsPlayer()
    {
        var (orchestrator, repo) = CreatePlayerOrchestrator();

        var result = await orchestrator.RegisterAsync("magnus", "hunter2pass");

        Assert.Equal("magnus", result.Username);

        var added = Assert.Single(repo.AddedPlayers);
        Assert.Equal(result.Id, added.Id);
        Assert.NotEqual("hunter2pass", added.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("hunter2pass", added.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_UsernameAlreadyTaken_ThrowsUsernameTakenException()
    {
        var (orchestrator, _) = CreatePlayerOrchestrator();
        await orchestrator.RegisterAsync("magnus", "hunter2pass");

        await Assert.ThrowsAsync<UsernameTakenException>(() => orchestrator.RegisterAsync("magnus", "otherpassword"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("this-username-is-way-too-long-to-be-valid")]
    public async Task RegisterAsync_UsernameWrongLength_ThrowsInvalidUsernameException(string username)
    {
        var (orchestrator, _) = CreatePlayerOrchestrator();

        await Assert.ThrowsAsync<InvalidUsernameException>(() => orchestrator.RegisterAsync(username, "hunter2pass"));
    }

    [Fact]
    public async Task RegisterAsync_ShortPassword_ThrowsWeakPasswordException()
    {
        var (orchestrator, _) = CreatePlayerOrchestrator();

        await Assert.ThrowsAsync<WeakPasswordException>(() => orchestrator.RegisterAsync("magnus", "short"));
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsPlayer()
    {
        var (orchestrator, _) = CreatePlayerOrchestrator();
        var registered = await orchestrator.RegisterAsync("magnus", "hunter2pass");

        var result = await orchestrator.LoginAsync("magnus", "hunter2pass");

        Assert.Equal(registered.Id, result.Id);
        Assert.Equal("magnus", result.Username);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var (orchestrator, _) = CreatePlayerOrchestrator();
        await orchestrator.RegisterAsync("magnus", "hunter2pass");

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => orchestrator.LoginAsync("magnus", "wrongpassword"));
    }

    [Fact]
    public async Task LoginAsync_UnknownUsername_ThrowsInvalidCredentialsException()
    {
        var (orchestrator, _) = CreatePlayerOrchestrator();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => orchestrator.LoginAsync("ghost", "hunter2pass"));
    }
}
