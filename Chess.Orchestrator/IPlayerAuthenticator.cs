namespace Chess.Orchestrator;

public interface IPlayerAuthenticator
{
    Task<AuthenticatedPlayer> RegisterAsync(string username, string password, CancellationToken ct = default);

    Task<AuthenticatedPlayer> LoginAsync(string username, string password, CancellationToken ct = default);

    Task<AuthenticatedPlayer> RefreshAsync(Guid playerId, CancellationToken ct = default);
}
