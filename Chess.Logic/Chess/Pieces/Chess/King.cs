using Chess.Logic.Board;

namespace Chess.Logic.Pieces.Chess;

public class King(Team team) : Piece(team)
{
    public override char PieceCode => 'K';
    public bool HasMoved { get; set; }

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

        AddCastlingMoves(board, pieceCord, moves);

        return [.. moves];
    }

    // Castling never captures, so it never threatens a square: attacked squares are
    // just the plain adjacent ones. Reusing PossibleMoves here would recurse into
    // AddCastlingMoves -> IsSquareAttacked -> GetAttackedSquares forever.
    public override IEnumerable<PieceCord> GetAttackedSquares(ChessBoard board, PieceCord pieceCord)
    {
        foreach (var (dx, dy) in Directions)
        {
            int x = pieceCord.X + dx;
            int y = pieceCord.Y + dy;
            if (x >= 0 && x < board.Spots.Length && y >= 0 && y < board.Spots[x].Length)
                yield return new PieceCord(x, y);
        }
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

    private void AddCastlingMoves(ChessBoard board, PieceCord kingCord, List<PieceMove> pieceMoves)
    {
        if (HasMoved || board.IsSquareAttacked(kingCord, OpponentTeam))
            return;

        TryAddCastle(board, kingCord, rookX: board.Spots.Length - 1, step: 1, pieceMoves);
        TryAddCastle(board, kingCord, rookX: 0, step: -1, pieceMoves);
    }

    private void TryAddCastle(ChessBoard board, PieceCord kingCord, int rookX, int step, List<PieceMove> pieceMoves)
    {
        if (board.Spots[rookX][kingCord.Y].Piece is not Rook rook || rook.Team != Team || rook.HasMoved)
            return;

        for (int x = kingCord.X + step; x != rookX; x += step)
        {
            if (board.Spots[x][kingCord.Y].Piece != null)
                return;
        }

        for (int i = 1; i <= 2; i++)
        {
            var passThrough = new PieceCord(kingCord.X + step * i, kingCord.Y);
            if (board.IsSquareAttacked(passThrough, OpponentTeam))
                return;
        }

        pieceMoves.Add(new PieceMove(kingCord, new PieceCord(kingCord.X + step * 2, kingCord.Y)));
    }
}
