namespace Chess.Model.Player;

public interface IPlayerOrchestrator
{
    Task<Player> RegisterAsync(string username, string password, CancellationToken ct = default);

    Task<Player> LoginAsync(string username, string password, CancellationToken ct = default);

    Task<Player> RefreshAsync(Guid playerId, CancellationToken ct = default);
}
