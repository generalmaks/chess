using Chess.Dal.Entities;
using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Dal;

public class GameRepository(ChessDbContext db) : IGameRepository
{
    public async Task AddGameAsync(GameEntity game, CancellationToken ct = default)
    {
        db.Games.Add(game);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddMoveAsync(MoveEntity move, CancellationToken ct = default)
    {
        db.Moves.Add(move);
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

    public async Task EndGameAsync(Guid gameId, GameResult result, DateTime endedAtUtc, CancellationToken ct = default)
    {
        var game = await db.Games.FindAsync([gameId], ct)
            ?? throw new InvalidOperationException($"Game {gameId} is missing from the database.");

        game.Result = result;
        game.EndedAtUtc = endedAtUtc;

        await db.SaveChangesAsync(ct);
    }
}
