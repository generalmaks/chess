using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests.Board;

public class EnPassantTests
{
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
    public void MakeMove_EnPassantCapture_RemovesCapturedPawnAndMovesCapturingPawn()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White) { HasMadeFirstMove = true }, 3, 4);
        board.Place(new Pawn(Team.Black), 4, 6);
        board.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)), Team.Black);

        board.MakeMove(new PieceMove(new PieceCord(3, 4), new PieceCord(4, 5)), Team.White);

        Assert.IsType<Pawn>(board.Spots[4][5].Piece);
        Assert.False(board.Spots[3][4].IsSpotOccupied);
        Assert.False(board.Spots[4][4].IsSpotOccupied);
        Assert.True(board.MoveHistory[^1].IsEnPassant);
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
