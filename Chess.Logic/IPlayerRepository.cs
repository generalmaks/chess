namespace Chess.Logic;

public interface IPlayerRepository
{
    Task<Player?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<Player?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddPlayerAsync(Player player, CancellationToken ct = default);

    Task UpdateRatingAsync(Guid playerId, int newRating, CancellationToken ct = default);
}
