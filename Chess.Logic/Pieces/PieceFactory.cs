using Chess.Logic.Pieces.Chess;

namespace Chess.Logic.Pieces;

public static class PieceFactory
{
    public static Piece CreatePiece(char pieceCode, Team team) => pieceCode switch
    {
        'P' => new Pawn(team),
        'N' => new Knight(team),
        'B' => new Bishop(team),
        'R' => new Rook(team),
        'Q' => new Queen(team),
        _ => throw new ArgumentException($"Unknown piece code '{pieceCode}'")
    };
}