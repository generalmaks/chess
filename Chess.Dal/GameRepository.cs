using Chess.Dal.Entities;
using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Dal;

public class GameRepository(ChessDbContext db) : IGameRepository
{
    public async Task AddGameAsync(NewGame game, CancellationToken ct = default)
    {
        db.Games.Add(new GameEntity
        {
            Id = game.Id,
            CreatedAtUtc = game.CreatedAtUtc,
            WhitePlayerId = game.WhitePlayerId,
            BlackPlayerId = game.BlackPlayerId,
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task AddMoveAsync(RecordedMove move, CancellationToken ct = default)
    {
        db.Moves.Add(new MoveEntity
        {
            GameId = move.GameId,
            Ply = move.Ply,
            Team = move.Team,
            PieceCode = move.PieceCode,
            FromX = move.FromX,
            FromY = move.FromY,
            ToX = move.ToX,
            ToY = move.ToY,
            PromotionPieceCode = move.PromotionPieceCode,
            IsCastling = move.IsCastling,
            IsEnPassant = move.IsEnPassant,
            PlayedAtUtc = move.PlayedAtUtc,
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task AssignPlayerAsync(Guid gameId, Team team, Guid playerId, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([gameId], ct)
            ?? throw new InvalidOperationException($"Game {gameId} is missing from the database.");

        if (team == Team.White) game.WhitePlayerId = playerId;
        else game.BlackPlayerId = playerId;

        await db.SaveChangesAsync(ct);
    }

    public async Task SetStartingRatingsAsync(Guid gameId, int whiteEloBefore, int blackEloBefore, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([gameId], ct)
            ?? throw new InvalidOperationException($"Game {gameId} is missing from the database.");

        game.WhiteEloBefore = whiteEloBefore;
        game.BlackEloBefore = blackEloBefore;

        await db.SaveChangesAsync(ct);
    }

    public async Task EndGameAsync(Guid gameId, GameResult result, DateTime endedAtUtc, int? whiteEloAfter = null, int? blackEloAfter = null, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([gameId], ct)
            ?? throw new InvalidOperationException($"Game {gameId} is missing from the database.");

        game.Result = result;
        game.EndedAtUtc = endedAtUtc;
        game.WhiteEloAfter = whiteEloAfter;
        game.BlackEloAfter = blackEloAfter;

        await db.SaveChangesAsync(ct);
    }
}
