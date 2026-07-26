using Chess.Logic.Board;

namespace Chess.Logic.Pieces.Chess;

public abstract class Piece
{
    public abstract char PieceCode { get; }
    public abstract PieceMove[] PossibleMoves(ChessBoard board, PieceCord pieceCord);
    public Team Team { get; }

    protected Piece(Team team)
    {
        Team = team;
    }
}