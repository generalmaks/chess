using Chess.Logic.Board;

namespace Chess.Logic.Pieces.Chess;

public class King(Team team) : Piece(team)
{
    public override char PieceCode => 'K';

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
            TryAddMove(board, pieceCord, pieceCord.X + dx, pieceCord.Y + dy, moves);
        }

        return [.. moves];
    }

    private void TryAddMove(ChessBoard board, PieceCord pieceCord, int targetX, int targetY, List<PieceMove> pieceMoves)
    {
        if (targetX < 0 || targetX >= board.Spots.Length)
            return;
        if (targetY < 0 || targetY >= board.Spots[targetX].Length)
            return;

        var targetSpot = board.Spots[targetX][targetY];
        if (targetSpot.Piece != null && !IsEnemyPiece(targetSpot.Piece))
            return;

        pieceMoves.Add(new PieceMove(pieceCord, new PieceCord(targetX, targetY)));
    }
}
