using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests;

public class KingTests
{
    [Fact]
    public void PossibleMoves_CenterOfEmptyBoard_ReturnsAllEightMoves()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(king, coord.X, coord.Y);

        var moves = king.PossibleMoves(board, coord);

        Assert.Equal(8, moves.Length);
    }

    [Fact]
    public void PossibleMoves_Corner_ReturnsOnlyThreeMoves()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(king, coord.X, coord.Y);

        var moves = king.PossibleMoves(board, coord);

        Assert.Equivalent(new[]
        {
            new PieceCord(1, 0), new PieceCord(0, 1), new PieceCord(1, 1)
        }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_FriendlyPieceOnTarget_ExcludesThatSquare()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(king, coord.X, coord.Y);
        board.Place(new Pawn(Team.White), 4, 5);

        var moves = king.PossibleMoves(board, coord);

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 5));
        Assert.Equal(7, moves.Length);
    }

    [Fact]
    public void PossibleMoves_EnemyPieceOnTarget_IncludesCapture()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(king, coord.X, coord.Y);
        board.Place(new Pawn(Team.Black), 4, 5);

        var moves = king.PossibleMoves(board, coord);

        Assert.Contains(moves, m => m.To == new PieceCord(4, 5));
        Assert.Equal(8, moves.Length);
    }
}
