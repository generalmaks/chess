using Chess.Logic.Pieces.Chess;

namespace Chess.Logic;

public interface IGameRepository
{
    Task AddGameAsync(NewGame game, CancellationToken ct = default);

    Task AddMoveAsync(RecordedMove move, CancellationToken ct = default);

    Task AssignPlayerAsync(Guid gameId, Team team, Guid playerId, CancellationToken ct = default);

    Task SetStartingRatingsAsync(Guid gameId, int whiteEloBefore, int blackEloBefore, CancellationToken ct = default);

    Task EndGameAsync(Guid gameId, GameResult result, DateTime endedAtUtc, int? whiteEloAfter = null, int? blackEloAfter = null, CancellationToken ct = default);
}
