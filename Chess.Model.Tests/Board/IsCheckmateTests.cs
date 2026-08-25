using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class IsCheckmateTests
{
    [Fact]
    public void IsCheckmate_BackRankMate_ReturnsTrue()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Pawn(Team.White), 3, 1);
        board.Place(new Pawn(Team.White), 4, 1);
        board.Place(new Pawn(Team.White), 5, 1);
        board.Place(new Rook(Team.Black), 0, 0);

        Assert.True(board.IsInCheck(Team.White));
        Assert.True(board.IsCheckmate(Team.White));
    }

    [Fact]
    public void IsCheckmate_CheckCanBeBlocked_ReturnsFalse()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.White), 0, 3);
        board.Place(new Rook(Team.Black), 4, 7);

        Assert.True(board.IsInCheck(Team.White));
        Assert.False(board.IsCheckmate(Team.White));
    }

    [Fact]
    public void IsCheckmate_NotInCheck_ReturnsFalse()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);

        Assert.False(board.IsCheckmate(Team.White));
    }
}
