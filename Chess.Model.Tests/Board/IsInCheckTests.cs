using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class IsInCheckTests
{
    [Fact]
    public void IsInCheck_KingAttackedByEnemyRook_ReturnsTrue()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.Black), 4, 7);

        Assert.True(board.IsInCheck(Team.White));
    }

    [Fact]
    public void IsInCheck_KingNotAttacked_ReturnsFalse()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.Black), 0, 7);

        Assert.False(board.IsInCheck(Team.White));
    }

    [Fact]
    public void IsInCheck_NoKingOnBoard_ReturnsFalse()
    {
        var board = TestBoard.Empty();
        board.Place(new Rook(Team.Black), 4, 7);

        Assert.False(board.IsInCheck(Team.White));
    }
}
