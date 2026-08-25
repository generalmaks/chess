using Chess.Model.Player;

namespace Chess.Orchestrator.Player;

public class PlayerOrchestrator(IPlayerRepository repository) : IPlayerOrchestrator
{
    public async Task<Model.Player.Player> RegisterAsync(string username, string password, CancellationToken ct = default)
    {
        username = username.Trim();

        if (username.Length is < 3 or > 32)
            throw new InvalidUsernameException();

        if (password.Length < 8)
            throw new WeakPasswordException();

        if (await repository.GetByUsernameAsync(username, ct) is not null)
            throw new UsernameTakenException();

        var player = new Model.Player.Player
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await repository.AddPlayerAsync(player, ct);
        return player;
    }

    public async Task<Model.Player.Player> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var player = await repository.GetByUsernameAsync(username.Trim(), ct);
        if (player is null || !BCrypt.Net.BCrypt.Verify(password, player.PasswordHash))
            throw new InvalidCredentialsException();

        return player;
    }

    public async Task<Model.Player.Player> RefreshAsync(Guid playerId, CancellationToken ct = default) =>
        await repository.GetByIdAsync(playerId, ct) ?? throw new InvalidCredentialsException();
}
