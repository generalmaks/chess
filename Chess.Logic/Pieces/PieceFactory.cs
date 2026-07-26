using Chess.Logic.Pieces.Chess;

namespace Chess.Logic.Pieces;

public static class PieceFactory
{
    public static Piece CreatePiece(char pieceCode, Team team) => pieceCode switch
    {
        'P' => new Pawn(team),
        'N' => new Knight(team),
        _ => throw new ArgumentException($"Unknown piece code '{pieceCode}'")
    };
}