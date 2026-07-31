using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests.Pieces;

public class PawnTests
{
    [Fact]
    public void PossibleMoves_FirstMove_IncludesOneAndTwoStepAdvance()
    {
        var board = TestBoard.Empty();
        var pawn = new Pawn(Team.White);
        var coord = new PieceCord(4, 1);
        board.Place(pawn, coord.X, coord.Y);

        var moves = pawn.PossibleMoves(board, coord);

        Assert.Equivalent(new[]
        {
            new PieceCord(4, 2),
            new PieceCord(4, 3)
        }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_AfterFirstMove_OnlyOneStepAdvance()
    {
        var board = TestBoard.Empty();
        var pawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        var coord = new PieceCord(4, 2);
        board.Place(pawn, coord.X, coord.Y);

        var moves = pawn.PossibleMoves(board, coord);

        Assert.Equivalent(new[] { new PieceCord(4, 3) }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_BlockedDirectlyAhead_NoForwardMoves()
    {
        var board = TestBoard.Empty();
        var pawn = new Pawn(Team.White);
        var coord = new PieceCord(4, 1);
        board.Place(pawn, coord.X, coord.Y);
        board.Place(new Knight(Team.Black), 4, 2);

        var moves = pawn.PossibleMoves(board, coord);

        Assert.Empty(moves);
    }

    [Fact]
    public void PossibleMoves_BlockedDirectlyAheadWithEnemyOnDiagonal_StillIncludesCapture()
    {
        var board = TestBoard.Empty();
        var pawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        var coord = new PieceCord(4, 3);
        board.Place(pawn, coord.X, coord.Y);
        board.Place(new Pawn(Team.Black), 4, 4);
        board.Place(new Pawn(Team.Black), 3, 4);

        var moves = pawn.PossibleMoves(board, coord);

        Assert.Equivalent(new[] { new PieceCord(3, 4) }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_EnemyOnDiagonal_IncludesCapture()
    {
        var board = TestBoard.Empty();
        var pawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        var coord = new PieceCord(4, 1);
        board.Place(pawn, coord.X, coord.Y);
        board.Place(new Knight(Team.Black), 3, 2);
        board.Place(new Knight(Team.Black), 5, 2);

        var moves = pawn.PossibleMoves(board, coord);

        Assert.Equivalent(new[]
        {
            new PieceCord(4, 2),
            new PieceCord(3, 2),
            new PieceCord(5, 2)
        }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_FriendlyOnDiagonal_DoesNotIncludeCapture()
    {
        var board = TestBoard.Empty();
        var pawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        var coord = new PieceCord(4, 1);
        board.Place(pawn, coord.X, coord.Y);
        board.Place(new Knight(Team.White), 3, 2);

        var moves = pawn.PossibleMoves(board, coord);

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(3, 2));
    }

    [Fact]
    public void PossibleMoves_AtFarEdgeOfBoard_ReturnsNoMoves()
    {
        var board = TestBoard.Empty();
        var pawn = new Pawn(Team.Black);
        var coord = new PieceCord(4, 0);
        board.Place(pawn, coord.X, coord.Y);

        var moves = pawn.PossibleMoves(board, coord);

        Assert.Empty(moves);
    }
}
