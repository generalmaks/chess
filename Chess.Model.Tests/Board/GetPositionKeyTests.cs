using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class GetPositionKeyTests
{
    [Fact]
    public void GetPositionKey_DifferentSideToMove_Differs()
    {
        var board = new ChessBoard();

        Assert.NotEqual(board.GetPositionKey(Team.White), board.GetPositionKey(Team.Black));
    }

    [Fact]
    public void GetPositionKey_SamePlacementWithoutCastlingRights_Differs()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.White), 7, 0);
        board.Place(new King(Team.Black), 4, 7);
        var withRights = board.GetPositionKey(Team.White);

        board.Place(new King(Team.White) { HasMoved = true }, 4, 0);

        Assert.NotEqual(withRights, board.GetPositionKey(Team.White));
    }

    [Fact]
    public void GetPositionKey_DoublePushWithLegalEnPassant_IncludesEnPassant()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 0, 0);
        board.Place(new King(Team.Black), 7, 7);
        board.Place(new Pawn(Team.White), 4, 1);
        board.Place(new Pawn(Team.Black), 3, 3);
        var before = board.GetPositionKey(Team.White);

        board.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), Team.White);
        var afterPush = board.GetPositionKey(Team.Black);

        Assert.Contains("e4", afterPush[^2..]);
        Assert.NotEqual(before, afterPush);
    }

    [Fact]
    public void GetPositionKey_DoublePushWithoutCapturingPawn_OmitsEnPassant()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 0, 0);
        board.Place(new King(Team.Black), 7, 7);
        board.Place(new Pawn(Team.White), 4, 1);

        board.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), Team.White);

        Assert.DoesNotContain('e', board.GetPositionKey(Team.Black)[64..]);
    }
}
