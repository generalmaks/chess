using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests;

public class QueenTests
{
    [Fact]
    public void PossibleMoves_FromCornerOnEmptyBoard_CombinesRookAndBishopMoves()
    {
        var board = TestBoard.Empty();
        var queen = new Queen(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(queen, coord.X, coord.Y);

        var moves = queen.PossibleMoves(board, coord);

        Assert.Equal(21, moves.Length);
        Assert.Contains(moves, m => m.To == new PieceCord(7, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(0, 7));
        Assert.Contains(moves, m => m.To == new PieceCord(7, 7));
    }

    [Fact]
    public void PossibleMoves_BlockedByFriendlyPiece_StopsBeforeIt()
    {
        var board = TestBoard.Empty();
        var queen = new Queen(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(queen, coord.X, coord.Y);
        board.Place(new Pawn(Team.White), 4, 6);

        var moves = queen.PossibleMoves(board, coord);

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 6));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 7));
        Assert.Contains(moves, m => m.To == new PieceCord(4, 5));
    }

    [Fact]
    public void PossibleMoves_EnemyPiece_IncludesCaptureButNotBeyond()
    {
        var board = TestBoard.Empty();
        var queen = new Queen(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(queen, coord.X, coord.Y);
        board.Place(new Pawn(Team.Black), 4, 6);

        var moves = queen.PossibleMoves(board, coord);

        Assert.Contains(moves, m => m.To == new PieceCord(4, 6));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 7));
    }
}
