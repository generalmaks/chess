using Chess.Logic;

namespace Chess.Dal.Entities;

public class GameDao
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public GameResult Result { get; set; } = GameResult.Ongoing;

    public Guid? WhitePlayerId { get; set; }
    public PlayerDao? WhitePlayer { get; set; }

    public Guid? BlackPlayerId { get; set; }
    public PlayerDao? BlackPlayer { get; set; }

    public int? WhiteEloBefore { get; set; }
    public int? BlackEloBefore { get; set; }
    public int? WhiteEloAfter { get; set; }
    public int? BlackEloAfter { get; set; }

    public List<MoveDao> Moves { get; set; } = [];
}
