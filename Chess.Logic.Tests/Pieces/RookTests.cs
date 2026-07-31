using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests;

public class RookTests
{
    [Fact]
    public void PossibleMoves_FromCornerOnEmptyBoard_SlidesAlongBothEdges()
    {
        var board = TestBoard.Empty();
        var rook = new Rook(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(rook, coord.X, coord.Y);

        var moves = rook.PossibleMoves(board, coord);

        var expected = Enumerable.Range(1, 7).Select(x => new PieceCord(x, 0))
            .Concat(Enumerable.Range(1, 7).Select(y => new PieceCord(0, y)));
        Assert.Equivalent(expected.ToArray(), moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_BlockedByFriendlyPiece_StopsBeforeIt()
    {
        var board = TestBoard.Empty();
        var rook = new Rook(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(rook, coord.X, coord.Y);
        board.Place(new Pawn(Team.White), 3, 0);

        var moves = rook.PossibleMoves(board, coord);

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(3, 0));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 0));
    }

    [Fact]
    public void PossibleMoves_BlockedByEnemyPiece_IncludesCaptureButNotBeyond()
    {
        var board = TestBoard.Empty();
        var rook = new Rook(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(rook, coord.X, coord.Y);
        board.Place(new Pawn(Team.Black), 3, 0);

        var moves = rook.PossibleMoves(board, coord);

        Assert.Contains(moves, m => m.To == new PieceCord(3, 0));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 0));
    }
}
