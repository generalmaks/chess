using Chess.Dal;
using Chess.Dal.Entities;
using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public readonly record struct DrawResponseResult(GameRoom Room, bool Accepted);

// Coordinates the in-memory GameStore (live sessions, one per active game) with
// Chess.Dal (durable game/move history). Transport layers (e.g. the SignalR hub)
// should depend on this instead of touching GameStore or IGameRepository directly.
public class GameOrchestrator(GameStore store, IGameRepository repository)
{
    public async Task<GameRoom> CreateGameAsync(CancellationToken ct = default)
    {
        var room = store.CreateGame();

        await repository.AddGameAsync(new GameEntity
        {
            Id = Guid.Parse(room.Id),
            CreatedAtUtc = DateTime.UtcNow,
        }, ct);

        return room;
    }

    public (GameRoom Room, Team Team) Join(string connectionId, string gameId, string token)
    {
        var room = store.GetGame(gameId) ?? throw new GameNotFoundException();
        var team = room.TeamForToken(token) ?? throw new InvalidTokenException();

        store.RegisterConnection(connectionId, gameId, team);
        return (room, team);
    }

    public async Task<GameRoom> MakeMoveAsync(string connectionId, PieceMove move, char? promotion, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);
        if (room.Session.CurrentTurn != team)
            throw new NotYourTurnException();

        room.Session.MakeMove(move, promotion);
        room.DrawOfferedBy = null;

        await PersistMoveAsync(room, team, ct);
        return room;
    }

    public async Task<GameRoom> ResignAsync(string connectionId, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);

        room.Session.Resign(team);
        await EndGameAsync(room, ct);

        return room;
    }

    public (GameRoom Room, Team Team) OfferDraw(string connectionId)
    {
        var (room, team) = GetActiveConnection(connectionId);
        room.DrawOfferedBy = team;
        return (room, team);
    }

    public async Task<DrawResponseResult> RespondToDrawAsync(string connectionId, bool accept, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);
        if (room.DrawOfferedBy is null || room.DrawOfferedBy == team)
            throw new NoDrawOfferException();

        room.DrawOfferedBy = null;

        if (!accept)
            return new DrawResponseResult(room, Accepted: false);

        room.Session.AgreeToDraw();
        await EndGameAsync(room, ct);

        return new DrawResponseResult(room, Accepted: true);
    }

    public void Disconnect(string connectionId) => store.RemoveConnection(connectionId);

    private (GameRoom Room, Team Team) GetActiveConnection(string connectionId)
    {
        var connection = store.GetConnection(connectionId) ?? throw new NoActiveConnectionException();
        var room = store.GetGame(connection.GameId) ?? throw new GameNotFoundException();
        return (room, connection.Team);
    }

    private async Task PersistMoveAsync(GameRoom room, Team team, CancellationToken ct)
    {
        var lastMove = room.Session.Board.MoveHistory[^1];

        await repository.AddMoveAsync(new MoveEntity
        {
            GameId = Guid.Parse(room.Id),
            Ply = room.Session.Board.MoveHistory.Count - 1,
            Team = team,
            PieceCode = lastMove.PieceCode,
            FromX = lastMove.Move.From.X,
            FromY = lastMove.Move.From.Y,
            ToX = lastMove.Move.To.X,
            ToY = lastMove.Move.To.Y,
            PromotionPieceCode = lastMove.PromotedTo,
            IsCastling = lastMove.IsCastling,
            IsEnPassant = lastMove.IsEnPassant,
            PlayedAtUtc = DateTime.UtcNow,
        }, ct);

        if (room.Session.Result != GameResult.Ongoing)
            await EndGameAsync(room, ct);
    }

    private Task EndGameAsync(GameRoom room, CancellationToken ct) =>
        repository.EndGameAsync(Guid.Parse(room.Id), room.Session.Result, DateTime.UtcNow, ct);
}
