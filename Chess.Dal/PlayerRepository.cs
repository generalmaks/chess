using Chess.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chess.Dal;

public class PlayerRepository(ChessDbContext db) : IPlayerRepository
{
    public Task<PlayerEntity?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.Username == username, ct);

    public Task<PlayerEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddPlayerAsync(PlayerEntity player, CancellationToken ct = default)
    {
        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRatingAsync(Guid playerId, int newRating, CancellationToken ct = default)
    {
        var player = await db.Players.FindAsync([playerId], ct)
            ?? throw new InvalidOperationException($"Player {playerId} is missing from the database.");

        player.EloRating = newRating;
        await db.SaveChangesAsync(ct);
    }
}
