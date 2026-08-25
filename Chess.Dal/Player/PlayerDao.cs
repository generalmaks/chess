using Chess.Dal.Chess;

namespace Chess.Dal.Player;

public class PlayerDao
{
    public const int DefaultEloRating = 1200;

    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public int EloRating { get; set; } = DefaultEloRating;
    public List<GameDao> GamesAsWhite { get; set; } = [];
    public List<GameDao> GamesAsBlack { get; set; } = [];
}
