using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public interface IGameOrchestrator
{
    Task<GameRoom> CreateGameAsync(Guid playerId, Team? preferredTeam = null, TimeControl? timeControl = null, CancellationToken ct = default);

    Task<(GameRoom Room, Team Team)> JoinAsync(string connectionId, string gameId, Guid playerId, CancellationToken ct = default);

    Task<GameRoom> MakeMoveAsync(string connectionId, PieceMove move, char? promotion, CancellationToken ct = default);

    Task<GameRoom> ResignAsync(string connectionId, CancellationToken ct = default);

    (GameRoom Room, Team Team) OfferDraw(string connectionId);

    Task<DrawResponseResult> RespondToDrawAsync(string connectionId, bool accept, CancellationToken ct = default);

    // Called when a player's connection drops for any reason (explicit leave, tab close,
    // network loss, ...). If they were mid-game with an opponent already seated, this
    // resigns them so the game doesn't hang open forever; otherwise it's a no-op cleanup.
    Task<GameRoom?> HandleDisconnectAsync(string connectionId, CancellationToken ct = default);

    Task<GameRoom?> CheckTimeoutAsync(string gameId, CancellationToken ct = default);
}
