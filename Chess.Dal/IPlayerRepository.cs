using Chess.Dal.Entities;

namespace Chess.Dal;

public interface IPlayerRepository
{
    Task<PlayerEntity?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<PlayerEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddPlayerAsync(PlayerEntity player, CancellationToken ct = default);

    Task UpdateRatingAsync(Guid playerId, int newRating, CancellationToken ct = default);
}
