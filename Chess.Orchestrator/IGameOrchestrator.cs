using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public interface IGameOrchestrator
{
    Task<GameRoom> CreateGameAsync(Guid playerId, Team? preferredTeam = null, CancellationToken ct = default);

    Task<(GameRoom Room, Team Team)> JoinAsync(string connectionId, string gameId, Guid playerId, CancellationToken ct = default);

    Task<GameRoom> MakeMoveAsync(string connectionId, PieceMove move, char? promotion, CancellationToken ct = default);

    Task<GameRoom> ResignAsync(string connectionId, CancellationToken ct = default);

    (GameRoom Room, Team Team) OfferDraw(string connectionId);

    Task<DrawResponseResult> RespondToDrawAsync(string connectionId, bool accept, CancellationToken ct = default);

    void Disconnect(string connectionId);
}
