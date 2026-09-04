using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Chess.Game;

public sealed record RecordedMove
{
    public required Guid GameId { get; init; }
    public required int Ply { get; init; }
    public required Team Team { get; init; }
    public required char PieceCode { get; init; }
    public required int FromX { get; init; }
    public required int FromY { get; init; }
    public required int ToX { get; init; }
    public required int ToY { get; init; }
    public char? PromotionPieceCode { get; init; }
    public bool IsCastling { get; init; }
    public bool IsEnPassant { get; init; }
    public required string San { get; init; }
    public required DateTime PlayedAtUtc { get; init; }
}
