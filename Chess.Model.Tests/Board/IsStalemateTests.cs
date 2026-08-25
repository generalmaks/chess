using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class IsStalemateTests
{
    [Fact]
    public void IsStalemate_KingHasNoMovesButNotInCheck_ReturnsTrue()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 0, 0);
        board.Place(new King(Team.Black), 2, 1);
        board.Place(new Queen(Team.Black), 1, 2);

        Assert.False(board.IsInCheck(Team.White));
        Assert.True(board.IsStalemate(Team.White));
    }

    [Fact]
    public void IsStalemate_WhileInCheck_ReturnsFalse()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Pawn(Team.White), 3, 1);
        board.Place(new Pawn(Team.White), 4, 1);
        board.Place(new Pawn(Team.White), 5, 1);
        board.Place(new Rook(Team.Black), 0, 0);

        Assert.False(board.IsStalemate(Team.White));
    }
}
