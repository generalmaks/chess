using Chess.Dal.Entities;
using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Dal;

public interface IGameRepository
{
    Task AddGameAsync(GameEntity game, CancellationToken ct = default);

    Task AddMoveAsync(MoveEntity move, CancellationToken ct = default);

    Task AssignPlayerAsync(Guid gameId, Team team, Guid playerId, CancellationToken ct = default);

    Task EndGameAsync(Guid gameId, GameResult result, DateTime endedAtUtc, CancellationToken ct = default);
}
