using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Microsoft.EntityFrameworkCore;

namespace Chess.Dal.Chess;

public class GameRepository(ChessDbContext db) : IGameRepository
{
    public async Task<PagedResult<GameHistoryEntry>> GetPlayerGamesAsync(Guid playerId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Games
            .AsNoTracking()
            .Where(g => g.WhitePlayerId == playerId || g.BlackPlayerId == playerId)
            .OrderByDescending(g => g.CreatedAtUtc);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GameHistoryEntry
            {
                Id = g.Id,
                CreatedAtUtc = g.CreatedAtUtc,
                EndedAtUtc = g.EndedAtUtc,
                Result = g.Result,
                WhitePlayerId = g.WhitePlayerId,
                WhitePlayerName = g.WhitePlayer!.Username,
                BlackPlayerId = g.BlackPlayerId,
                BlackPlayerName = g.BlackPlayer!.Username,
                WhiteEloBefore = g.WhiteEloBefore,
                BlackEloBefore = g.BlackEloBefore,
                WhiteEloAfter = g.WhiteEloAfter,
                BlackEloAfter = g.BlackEloAfter,
                MoveCount = g.Moves.Count,
            })
            .ToListAsync(ct);

        return new PagedResult<GameHistoryEntry>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<GameReview?> GetGameForReviewAsync(Guid gameId, CancellationToken ct = default)
    {
        var game = await db.Games
            .AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => new
            {
                g.Id,
                g.Result,
                g.WhitePlayerId,
                g.BlackPlayerId,
                WhiteName = g.WhitePlayer!.Username,
                BlackName = g.BlackPlayer!.Username,
                Moves = g.Moves
                    .OrderBy(m => m.Ply)
                    .Select(m => new GameReviewMove
                    {
                        FromX = m.FromX,
                        FromY = m.FromY,
                        ToX = m.ToX,
                        ToY = m.ToY,
                        PromotedTo = m.PromotionPieceCode,
                        San = m.San,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (game is null)
            return null;

        return new GameReview
        {
            Id = game.Id,
            Result = game.Result,
            WhitePlayerId = game.WhitePlayerId,
            WhitePlayerName = game.WhiteName,
            BlackPlayerId = game.BlackPlayerId,
            BlackPlayerName = game.BlackName,
            Moves = game.Moves,
        };
    }

    public async Task AddGameAsync(NewGame game, CancellationToken ct = default)
    {
        db.Games.Add(new GameDao
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
        db.Moves.Add(new MoveDao
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
            San = move.San,
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
