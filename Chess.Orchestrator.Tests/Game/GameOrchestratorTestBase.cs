using Chess.Orchestrator.Game;
using Chess.Orchestrator.Tests.Support;

namespace Chess.Orchestrator.Tests.Game;

public abstract class GameOrchestratorTestBase
{
    protected static readonly Guid WhitePlayerId = Guid.NewGuid();
    protected static readonly Guid BlackPlayerId = Guid.NewGuid();

    protected static (GameOrchestrator Orchestrator, MockGameRepository Repository) CreateOrchestrator()
    {
        var repository = new MockGameRepository();
        var orchestrator = new GameOrchestrator(new GameStore(), repository.Object, SeededPlayers().Object);
        return (orchestrator, repository);
    }

    private static MockPlayerRepository SeededPlayers()
    {
        var players = new MockPlayerRepository();
        players.AddedPlayers.Add(new Model.Player.Player { Id = WhitePlayerId, Username = "white", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow, EloRating = 1200 });
        players.AddedPlayers.Add(new Model.Player.Player { Id = BlackPlayerId, Username = "black", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow, EloRating = 1200 });
        return players;
    }

    // Seeds both players into the player repository so EndGameAsync has ratings to update.
    protected static (GameOrchestrator Orchestrator, MockGameRepository GameRepository, MockPlayerRepository PlayerRepository) CreateOrchestratorWithPlayers(
        int whiteRating = 1200, int blackRating = 1200)
    {
        var gameRepository = new MockGameRepository();
        var playerRepository = new MockPlayerRepository();
        playerRepository.AddedPlayers.Add(new Model.Player.Player { Id = WhitePlayerId, Username = "white", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow, EloRating = whiteRating });
        playerRepository.AddedPlayers.Add(new Model.Player.Player { Id = BlackPlayerId, Username = "black", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow, EloRating = blackRating });

        var orchestrator = new GameOrchestrator(new GameStore(), gameRepository.Object, playerRepository.Object);
        return (orchestrator, gameRepository, playerRepository);
    }

    private protected static (GameOrchestrator Orchestrator, MockGameRepository Repository, ManualTimeProvider Time) CreateOrchestratorWithClock()
    {
        var repository = new MockGameRepository();
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var orchestrator = new GameOrchestrator(new GameStore(), repository.Object, SeededPlayers().Object, time);
        return (orchestrator, repository, time);
    }
}
