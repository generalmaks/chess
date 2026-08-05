using Chess.Logic.Pieces.Chess;

namespace Chess.Dal.Entities;

public class MoveEntity
{
    public long Id { get; set; }

    public Guid GameId { get; set; }
    public GameEntity Game { get; set; } = null!;

    // 0-based order within the game; white moves on even plies, black on odd.
    public int Ply { get; set; }
    public Team Team { get; set; }
    public char PieceCode { get; set; }
    public int FromX { get; set; }
    public int FromY { get; set; }
    public int ToX { get; set; }
    public int ToY { get; set; }
    public char? PromotionPieceCode { get; set; }
    public bool IsCastling { get; set; }
    public bool IsEnPassant { get; set; }
    public DateTime PlayedAtUtc { get; set; }
}
