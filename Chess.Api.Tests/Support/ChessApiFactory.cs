using Chess.Orchestrator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Chess.Api.Tests.Support;

public class ChessApiFactory : WebApplicationFactory<Program>
{
    public class Switchboard
    {
        public IGameOrchestrator GameOrchestrator { get; set; } = Mock.Of<IGameOrchestrator>();
        public IPlayerAuthenticator Authenticator { get; set; } = Mock.Of<IPlayerAuthenticator>();
    }

    public ChessApiFactory()
    {
        Program.StartupFactory = config => new TestStartup(config);
    }

    public Switchboard Mocks => Services.GetRequiredService<Switchboard>();

    public (Mock<IGameOrchestrator> Orchestrator, Mock<IPlayerAuthenticator> Authenticator) ResetMocks()
    {
        var orchestrator = new Mock<IGameOrchestrator>();
        var authenticator = new Mock<IPlayerAuthenticator>();
        Mocks.GameOrchestrator = orchestrator.Object;
        Mocks.Authenticator = authenticator.Object;
        return (orchestrator, authenticator);
    }
}
