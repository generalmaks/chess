using Chess.Orchestrator.Player;

namespace Chess.Orchestrator.Tests.Player;

public class LoginAsyncTests : PlayerOrchestratorTestBase
{
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
