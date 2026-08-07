using Chess.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chess.Dal;

public class PlayerRepository(ChessDbContext db) : IPlayerRepository
{
    public Task<PlayerEntity?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.Username == username, ct);

    public async Task AddPlayerAsync(PlayerEntity player, CancellationToken ct = default)
    {
        db.Players.Add(player);
        await db.SaveChangesAsync(ct);
    }
}
