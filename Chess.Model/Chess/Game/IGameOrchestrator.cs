using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Chess.Game;

public readonly record struct DrawResponseResult(Game Game, bool Accepted);

public interface IGameOrchestrator
{
    Task<Game> CreateGameAsync(Guid playerId, Team? preferredTeam = null, TimeControl? timeControl = null, CancellationToken ct = default);

    Task<PagedResult<GameHistoryEntry>> GetPlayerGamesAsync(Guid playerId, int page, int pageSize, CancellationToken ct = default);

    Task<GameReview> GetGameReviewAsync(Guid gameId, CancellationToken ct = default);

    Task<(Game Game, Team Team)> JoinAsync(string connectionId, string gameId, Guid playerId, CancellationToken ct = default);

    Task<Game> MakeMoveAsync(string connectionId, PieceMove move, char? promotion, CancellationToken ct = default);

    Task<Game> ResignAsync(string connectionId, CancellationToken ct = default);

    (Game Game, Team Team) OfferDraw(string connectionId);

    PieceMove[] GetLegalMoves(string connectionId, PieceCord from);

    Task<DrawResponseResult> RespondToDrawAsync(string connectionId, bool accept, CancellationToken ct = default);

    Task<Game?> HandleDisconnectAsync(string connectionId, CancellationToken ct = default);

    Task<Game?> CheckTimeoutAsync(string gameId, CancellationToken ct = default);
}
