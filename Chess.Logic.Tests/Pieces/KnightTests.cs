using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests;

public class KnightTests
{
    [Fact]
    public void PossibleMoves_CenterOfEmptyBoard_ReturnsAllEightMoves()
    {
        var board = TestBoard.Empty();
        var knight = new Knight(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(knight, coord.X, coord.Y);

        var moves = knight.PossibleMoves(board, coord);

        Assert.Equivalent(new[]
        {
            new PieceCord(5, 6), new PieceCord(6, 5), new PieceCord(6, 3), new PieceCord(5, 2),
            new PieceCord(3, 2), new PieceCord(2, 3), new PieceCord(2, 5), new PieceCord(3, 6)
        }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_Corner_ReturnsOnlyInBoundsMoves()
    {
        var board = TestBoard.Empty();
        var knight = new Knight(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(knight, coord.X, coord.Y);

        var moves = knight.PossibleMoves(board, coord);

        Assert.Equivalent(new[] { new PieceCord(1, 2), new PieceCord(2, 1) }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_FriendlyPieceOnTarget_ExcludesThatSquare()
    {
        var board = TestBoard.Empty();
        var knight = new Knight(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(knight, coord.X, coord.Y);
        board.Place(new Pawn(Team.White), 5, 6);

        var moves = knight.PossibleMoves(board, coord);

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(5, 6));
        Assert.Equal(7, moves.Length);
    }

    [Fact]
    public void PossibleMoves_EnemyPieceOnTarget_IncludesCapture()
    {
        var board = TestBoard.Empty();
        var knight = new Knight(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(knight, coord.X, coord.Y);
        board.Place(new Pawn(Team.Black), 5, 6);

        var moves = knight.PossibleMoves(board, coord);

        Assert.Contains(moves, m => m.To == new PieceCord(5, 6));
        Assert.Equal(8, moves.Length);
    }
}
