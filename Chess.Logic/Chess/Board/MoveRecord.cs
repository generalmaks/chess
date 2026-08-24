namespace Chess.Logic.Board;

public record MoveRecord(char PieceCode, PieceMove Move, bool IsCastling, char? PromotedTo = null, bool IsEnPassant = false);
