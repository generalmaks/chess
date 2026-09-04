using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Dal.Chess;

public class MoveDao
{
    public long Id { get; set; }

    public Guid GameId { get; set; }
    public GameDao Game { get; set; } = null!;
    
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
    public string San { get; set; } = "";
    public DateTime PlayedAtUtc { get; set; }
}
