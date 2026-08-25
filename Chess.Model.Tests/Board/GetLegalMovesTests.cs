using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class GetLegalMovesTests
{
    [Fact]
    public void GetLegalMoves_MoveWouldExposeKingToCheck_IsExcluded()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Bishop(Team.White), 4, 3);
        board.Place(new Rook(Team.Black), 4, 7);

        var moves = board.GetLegalMoves(new PieceCord(4, 3));

        Assert.Empty(moves);
    }

    [Fact]
    public void GetLegalMoves_WhileInCheck_OnlyMovesThatResolveCheckAreLegal()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.White), 0, 3);
        board.Place(new Rook(Team.Black), 4, 7);

        var rookMoves = board.GetLegalMoves(new PieceCord(0, 3));
        var kingMoves = board.GetLegalMoves(new PieceCord(4, 0));

        Assert.Contains(rookMoves, m => m.To == new PieceCord(4, 3));
        Assert.DoesNotContain(rookMoves, m => m.To == new PieceCord(0, 5));
        Assert.Contains(kingMoves, m => m.To == new PieceCord(3, 0));
    }
}
