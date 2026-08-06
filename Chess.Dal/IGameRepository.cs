using Chess.Dal.Entities;
using Chess.Logic;

namespace Chess.Dal;

// Narrow persistence contract for chess games/moves, so consumers (e.g. Chess.Orchestrator)
// can depend on this instead of ChessDbContext directly and be unit tested against a mock.
public interface IGameRepository
{
    Task AddGameAsync(GameEntity game, CancellationToken ct = default);

    Task AddMoveAsync(MoveEntity move, CancellationToken ct = default);

    Task EndGameAsync(Guid gameId, GameResult result, DateTime endedAtUtc, CancellationToken ct = default);
}
