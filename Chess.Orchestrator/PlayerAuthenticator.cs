using Chess.Dal;
using Chess.Dal.Entities;

namespace Chess.Orchestrator;

public class PlayerAuthenticator(IPlayerRepository repository) : IPlayerAuthenticator
{
    public async Task<AuthenticatedPlayer> RegisterAsync(string username, string password, CancellationToken ct = default)
    {
        username = username.Trim();

        if (username.Length is < 3 or > 32)
            throw new InvalidUsernameException();

        if (password.Length < 8)
            throw new WeakPasswordException();

        if (await repository.GetByUsernameAsync(username, ct) is not null)
            throw new UsernameTakenException();

        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await repository.AddPlayerAsync(player, ct);
        return new AuthenticatedPlayer(player.Id, player.Username);
    }

    public async Task<AuthenticatedPlayer> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var player = await repository.GetByUsernameAsync(username.Trim(), ct);
        if (player is null || !BCrypt.Net.BCrypt.Verify(password, player.PasswordHash))
            throw new InvalidCredentialsException();

        return new AuthenticatedPlayer(player.Id, player.Username);
    }
}
