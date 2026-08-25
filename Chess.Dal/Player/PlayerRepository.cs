using Chess.Dal.Entities;
using Chess.Logic;
using Microsoft.EntityFrameworkCore;

namespace Chess.Dal;

public class PlayerRepository(ChessDbContext db) : IPlayerRepository
{
    public async Task<Player?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var entity = await db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.Username == username, ct);
        return entity is null ? null : ToPlayer(entity);
    }

    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null : ToPlayer(entity);
    }

    public async Task AddPlayerAsync(Player player, CancellationToken ct = default)
    {
        db.Players.Add(new PlayerDao
        {
            Id = player.Id,
            Username = player.Username,
            PasswordHash = player.PasswordHash,
            CreatedAtUtc = player.CreatedAtUtc,
            EloRating = player.EloRating,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRatingAsync(Guid playerId, int newRating, CancellationToken ct = default)
    {
        var player = await db.Players.FindAsync([playerId], ct)
            ?? throw new InvalidOperationException($"Player {playerId} is missing from the database.");

        player.EloRating = newRating;
        await db.SaveChangesAsync(ct);
    }

    private static Player ToPlayer(PlayerDao dao) => new()
    {
        Id = dao.Id,
        Username = dao.Username,
        PasswordHash = dao.PasswordHash,
        CreatedAtUtc = dao.CreatedAtUtc,
        EloRating = dao.EloRating,
    };
}
