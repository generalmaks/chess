using Chess.Dal;
using Chess.Dal.Entities;
using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public readonly record struct DrawResponseResult(GameRoom Room, bool Accepted);

public class GameOrchestrator(GameStore store, ChessDbContext db)
{
    public async Task<GameRoom> CreateGameAsync(CancellationToken ct = default)
    {
        var room = store.CreateGame();

        db.Games.Add(new GameEntity
        {
            Id = Guid.Parse(room.Id),
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

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
        await PersistGameEndAsync(room, ct);

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
        await PersistGameEndAsync(room, ct);

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

        db.Moves.Add(new MoveEntity
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
        });

        if (room.Session.Result != GameResult.Ongoing)
            await MarkGameEndedAsync(room, ct);
        else
            await db.SaveChangesAsync(ct);
    }

    private async Task PersistGameEndAsync(GameRoom room, CancellationToken ct) => await MarkGameEndedAsync(room, ct);

    private async Task MarkGameEndedAsync(GameRoom room, CancellationToken ct)
    {
        var gameId = Guid.Parse(room.Id);
        var game = await db.Games.FindAsync([gameId], ct)
            ?? throw new InvalidOperationException($"Game {gameId} is missing from the database.");

        game.Result = room.Session.Result;
        game.EndedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
