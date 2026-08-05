using Chess.Logic;

namespace Chess.Dal.Entities;

public class GameEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public GameResult Result { get; set; } = GameResult.Ongoing;

    public Guid? WhitePlayerId { get; set; }
    public PlayerEntity? WhitePlayer { get; set; }

    public Guid? BlackPlayerId { get; set; }
    public PlayerEntity? BlackPlayer { get; set; }

    public List<MoveEntity> Moves { get; set; } = [];
}
