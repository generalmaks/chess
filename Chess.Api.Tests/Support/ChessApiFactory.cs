using Chess.Logic;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Chess.Api.Tests.Support;

public class ChessApiFactory : WebApplicationFactory<Program>
{
    public class Switchboard
    {
        public IGameOrchestrator GameOrchestrator { get; set; } = Mock.Of<IGameOrchestrator>();
        public IPlayerOrchestrator PlayerOrchestrator { get; set; } = Mock.Of<IPlayerOrchestrator>();
    }

    public ChessApiFactory()
    {
        Program.StartupFactory = config => new TestStartup(config);
    }

    public Switchboard Mocks => Services.GetRequiredService<Switchboard>();

    public (Mock<IGameOrchestrator> GameOrchestrator, Mock<IPlayerOrchestrator> PlayerOrchestrator) ResetMocks()
    {
        var gameOrchestrator = new Mock<IGameOrchestrator>();
        var playerOrchestrator = new Mock<IPlayerOrchestrator>();
        Mocks.GameOrchestrator = gameOrchestrator.Object;
        Mocks.PlayerOrchestrator = playerOrchestrator.Object;
        return (gameOrchestrator, playerOrchestrator);
    }
}
