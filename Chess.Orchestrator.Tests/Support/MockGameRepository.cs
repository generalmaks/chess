using Chess.Dal;
using Chess.Dal.Entities;
using Chess.Logic;
using Chess.Logic.Pieces.Chess;
using Moq;

namespace Chess.Orchestrator.Tests.Support;

// Wraps a Moq mock of IGameRepository so tests can assert what would have been persisted
// without touching a real database, ChessDbContext, or the EF InMemory provider.
public class MockGameRepository
{
    public Mock<IGameRepository> Mock { get; } = new();
    public IGameRepository Object => Mock.Object;

    public List<GameEntity> AddedGames { get; } = [];
    public List<MoveEntity> AddedMoves { get; } = [];

    public MockGameRepository()
    {
        Mock.Setup(r => r.AddGameAsync(It.IsAny<GameEntity>(), It.IsAny<CancellationToken>()))
            .Callback<GameEntity, CancellationToken>((game, _) => AddedGames.Add(game))
            .Returns(Task.CompletedTask);

        Mock.Setup(r => r.AddMoveAsync(It.IsAny<MoveEntity>(), It.IsAny<CancellationToken>()))
            .Callback<MoveEntity, CancellationToken>((move, _) => AddedMoves.Add(move))
            .Returns(Task.CompletedTask);

        Mock.Setup(r => r.AssignPlayerAsync(It.IsAny<Guid>(), It.IsAny<Team>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Team, Guid, CancellationToken>((gameId, team, playerId, _) =>
            {
                var game = AddedGames.Single(g => g.Id == gameId);
                if (team == Team.White) game.WhitePlayerId = playerId;
                else game.BlackPlayerId = playerId;
            })
            .Returns(Task.CompletedTask);

        Mock.Setup(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, GameResult, DateTime, CancellationToken>((gameId, result, endedAtUtc, _) =>
            {
                var game = AddedGames.Single(g => g.Id == gameId);
                game.Result = result;
                game.EndedAtUtc = endedAtUtc;
            })
            .Returns(Task.CompletedTask);
    }
}
