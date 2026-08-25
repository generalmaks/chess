using Chess.Orchestrator.Player;
using Chess.Orchestrator.Tests.Support;

namespace Chess.Orchestrator.Tests.Player;

public abstract class PlayerOrchestratorTestBase
{
    protected static (PlayerOrchestrator Orchestrator, MockPlayerRepository Repository) CreatePlayerOrchestrator()
    {
        var repository = new MockPlayerRepository();
        var orchestrator = new PlayerOrchestrator(repository.Object);
        return (orchestrator, repository);
    }
}
