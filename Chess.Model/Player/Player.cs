namespace Chess.Model.Player;

public class Player
{
    public const int DefaultEloRating = 1200;

    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public int EloRating { get; set; } = DefaultEloRating;
}
