using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Pieces;

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

    [Fact]
    public void PossibleMoves_AfterEnemyTwoSquareAdvanceAdjacent_IncludesEnPassantCapture()
    {
        var board = TestBoard.Empty();
        var whitePawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        board.Place(whitePawn, 3, 4);
        board.Place(new Pawn(Team.Black), 4, 6);

        board.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)), Team.Black);
        var moves = whitePawn.PossibleMoves(board, new PieceCord(3, 4));

        Assert.Contains(moves, m => m.To == new PieceCord(4, 5));
    }

    [Fact]
    public void PossibleMoves_NotImmediatelyAfterTwoSquareAdvance_ExcludesEnPassant()
    {
        var board = TestBoard.Empty();
        var whitePawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        board.Place(whitePawn, 3, 4);
        board.Place(new Pawn(Team.Black), 4, 6);
        board.Place(new Knight(Team.White), 0, 0);
        board.Place(new Knight(Team.Black), 0, 7);

        board.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)), Team.Black);
        board.MakeMove(new PieceMove(new PieceCord(0, 0), new PieceCord(1, 2)), Team.White);
        board.MakeMove(new PieceMove(new PieceCord(0, 7), new PieceCord(1, 5)), Team.Black);
        var moves = whitePawn.PossibleMoves(board, new PieceCord(3, 4));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 5));
    }

    [Fact]
    public void PossibleMoves_EnemyPawnAdvancedOneSquareOnly_ExcludesEnPassant()
    {
        var board = TestBoard.Empty();
        var whitePawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        board.Place(whitePawn, 3, 4);
        board.Place(new Pawn(Team.Black) { HasMadeFirstMove = true }, 4, 5);

        board.MakeMove(new PieceMove(new PieceCord(4, 5), new PieceCord(4, 4)), Team.Black);
        var moves = whitePawn.PossibleMoves(board, new PieceCord(3, 4));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 5));
    }

    [Fact]
    public void PossibleMoves_NotAdjacentFile_ExcludesEnPassant()
    {
        var board = TestBoard.Empty();
        var whitePawn = new Pawn(Team.White) { HasMadeFirstMove = true };
        board.Place(whitePawn, 1, 4);
        board.Place(new Pawn(Team.Black), 4, 6);

        board.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)), Team.Black);
        var moves = whitePawn.PossibleMoves(board, new PieceCord(1, 4));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 5));
    }
}
