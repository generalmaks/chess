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

    protected bool IsEnemyPiece(Piece piece) => piece.Team != Team;
    protected Team OpponentTeam => Team == Team.White ? Team.Black : Team.White;

    /// <summary>
    /// Squares this piece threatens, used for check/castling safety. Defaults to move
    /// targets; overridden where "attacks" and "moves" differ (pawns, and king to
    /// exclude castling, which never attacks a square).
    /// </summary>
    public virtual IEnumerable<PieceCord> GetAttackedSquares(ChessBoard board, PieceCord pieceCord) =>
        PossibleMoves(board, pieceCord).Select(m => m.To);
}