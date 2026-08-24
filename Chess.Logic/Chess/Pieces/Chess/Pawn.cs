using Chess.Logic.Board;

namespace Chess.Logic.Pieces.Chess;

public class Pawn(Team team) : Piece(team)
{
    public bool HasMadeFirstMove { get; set; }
    public override char PieceCode => 'P';
    /// <summary>
    /// Direction of pawn. 1 for white, -1 for black
    /// </summary>
    private int Direction { get; } = team == Team.White ? 1 : -1;

    public override PieceMove[] PossibleMoves(ChessBoard board, PieceCord pieceCoord)
    {
        var moves = new List<PieceMove>();
        int oneStepY = pieceCoord.Y + Direction;

        // bounds check
        if (oneStepY < 0 || oneStepY >= board.Spots[pieceCoord.X].Length)
            return [];

        if (!board.Spots[pieceCoord.X][oneStepY].IsSpotOccupied)
        {
            moves.Add(new PieceMove(pieceCoord, new PieceCord(pieceCoord.X, oneStepY)));

            if (!HasMadeFirstMove)
            {
                int twoStepY = pieceCoord.Y + Direction * 2;
                if (twoStepY >= 0 && twoStepY < board.Spots[pieceCoord.X].Length
                                  && !board.Spots[pieceCoord.X][twoStepY].IsSpotOccupied)
                {
                    moves.Add(new PieceMove(pieceCoord, new PieceCord(pieceCoord.X, twoStepY)));
                }
            }
        }

        DiagonalCapture(board, pieceCoord, moves);
        TryAddEnPassant(board, pieceCoord, moves);

        return [.. moves];
    }

    private void TryAddEnPassant(ChessBoard board, PieceCord pieceCord, List<PieceMove> pieceMoves)
    {
        if (board.MoveHistory.Count == 0)
            return;

        var lastMove = board.MoveHistory[^1];
        if (lastMove.PieceCode != 'P' || Math.Abs(lastMove.Move.To.Y - lastMove.Move.From.Y) != 2)
            return;
        if (lastMove.Move.To.Y != pieceCord.Y || Math.Abs(lastMove.Move.To.X - pieceCord.X) != 1)
            return;

        var passedPawnSpot = board.Spots[lastMove.Move.To.X][lastMove.Move.To.Y];
        if (passedPawnSpot.Piece is not Pawn passedPawn || !IsEnemyPiece(passedPawn))
            return;

        pieceMoves.Add(new PieceMove(pieceCord, new PieceCord(lastMove.Move.To.X, pieceCord.Y + Direction)));
    }

    private void DiagonalCapture(ChessBoard board, PieceCord pieceCord, List<PieceMove> pieceMoves)
    {
        int targetY = pieceCord.Y + Direction;
        if (targetY < 0 || targetY >= board.Spots[pieceCord.X].Length)
            return;

        TryAddDiagonalCapture(board, pieceCord, pieceCord.X + 1, targetY, pieceMoves);
        TryAddDiagonalCapture(board, pieceCord, pieceCord.X - 1, targetY, pieceMoves);
    }

    private void TryAddDiagonalCapture(ChessBoard board, PieceCord pieceCord, int targetX, int targetY, List<PieceMove> pieceMoves)
    {
        if (targetX < 0 || targetX >= board.Spots.Length)
            return;

        var targetSpot = board.Spots[targetX][targetY];
        if (targetSpot is { IsSpotOccupied: true, Piece: not null } && IsEnemyPiece(targetSpot.Piece))
        {
            pieceMoves.Add(new PieceMove(pieceCord, new PieceCord(targetX, targetY)));
        }
    }

    public override IEnumerable<PieceCord> GetAttackedSquares(ChessBoard board, PieceCord pieceCord)
    {
        int targetY = pieceCord.Y + Direction;
        if (targetY < 0 || targetY >= board.Spots[pieceCord.X].Length)
            yield break;

        if (pieceCord.X + 1 < board.Spots.Length)
            yield return new PieceCord(pieceCord.X + 1, targetY);
        if (pieceCord.X - 1 >= 0)
            yield return new PieceCord(pieceCord.X - 1, targetY);
    }
}