using Chess.Orchestrator.Player;

namespace Chess.Orchestrator.Tests.Player;

public class RegisterAsyncTests : PlayerOrchestratorTestBase
{
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
}
