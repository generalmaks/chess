using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests.Pieces;

public class BishopTests
{
    [Fact]
    public void PossibleMoves_FromCornerOnEmptyBoard_SlidesAlongSingleDiagonal()
    {
        var board = TestBoard.Empty();
        var bishop = new Bishop(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(bishop, coord.X, coord.Y);

        var moves = bishop.PossibleMoves(board, coord);

        Assert.Equivalent(new[]
        {
            new PieceCord(1, 1), new PieceCord(2, 2), new PieceCord(3, 3), new PieceCord(4, 4),
            new PieceCord(5, 5), new PieceCord(6, 6), new PieceCord(7, 7)
        }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_BlockedByFriendlyPiece_StopsBeforeIt()
    {
        var board = TestBoard.Empty();
        var bishop = new Bishop(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(bishop, coord.X, coord.Y);
        board.Place(new Pawn(Team.White), 3, 3);

        var moves = bishop.PossibleMoves(board, coord);

        Assert.Equivalent(new[] { new PieceCord(1, 1), new PieceCord(2, 2) }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_BlockedByEnemyPiece_IncludesCaptureButNotBeyond()
    {
        var board = TestBoard.Empty();
        var bishop = new Bishop(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(bishop, coord.X, coord.Y);
        board.Place(new Pawn(Team.Black), 3, 3);

        var moves = bishop.PossibleMoves(board, coord);

        Assert.Equivalent(new[]
        {
            new PieceCord(1, 1), new PieceCord(2, 2), new PieceCord(3, 3)
        }, moves.Select(m => m.To).ToArray());
    }
}
