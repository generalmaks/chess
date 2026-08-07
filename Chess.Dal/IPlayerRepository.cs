using Chess.Dal.Entities;

namespace Chess.Dal;

// Narrow persistence contract for player accounts, mirroring IGameRepository so
// Chess.Orchestrator can depend on this instead of ChessDbContext directly.
public interface IPlayerRepository
{
    Task<PlayerEntity?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task AddPlayerAsync(PlayerEntity player, CancellationToken ct = default);
}
