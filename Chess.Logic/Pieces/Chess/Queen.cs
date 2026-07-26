using Chess.Logic.Board;

namespace Chess.Logic.Pieces.Chess;

public class Queen(Team team) : Piece(team)
{
    public override char PieceCode => 'Q';

    private static readonly (int dx, int dy)[] Directions =
    [
        (1, 0), (-1, 0), (0, 1), (0, -1),
        (1, 1), (1, -1), (-1, -1), (-1, 1)
    ];

    public override PieceMove[] PossibleMoves(ChessBoard board, PieceCord pieceCord)
    {
        var moves = new List<PieceMove>();

        foreach (var (dx, dy) in Directions)
        {
            AddSlidingMoves(board, pieceCord, dx, dy, moves);
        }

        return [.. moves];
    }

    private void AddSlidingMoves(ChessBoard board, PieceCord pieceCord, int dx, int dy, List<PieceMove> pieceMoves)
    {
        int targetX = pieceCord.X + dx;
        int targetY = pieceCord.Y + dy;

        while (targetX >= 0 && targetX < board.Spots.Length && targetY >= 0 && targetY < board.Spots[targetX].Length)
        {
            var targetSpot = board.Spots[targetX][targetY];
            if (targetSpot.Piece != null)
            {
                if (IsEnemyPiece(targetSpot.Piece))
                    pieceMoves.Add(new PieceMove(pieceCord, new PieceCord(targetX, targetY)));
                break;
            }

            pieceMoves.Add(new PieceMove(pieceCord, new PieceCord(targetX, targetY)));
            targetX += dx;
            targetY += dy;
        }
    }
}
