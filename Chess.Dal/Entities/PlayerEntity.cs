namespace Chess.Dal.Entities;

public class PlayerEntity
{
    public const int DefaultEloRating = 1200;

    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public int EloRating { get; set; } = DefaultEloRating;
    public List<GameEntity> GamesAsWhite { get; set; } = [];
    public List<GameEntity> GamesAsBlack { get; set; } = [];
}
