using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests.Board;

public class CheckTests
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

    [Fact]
    public void MakeMove_MoveLeavesOwnKingInCheck_ThrowsMoveLeavesKingInCheckException()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Bishop(Team.White), 4, 3);
        board.Place(new Rook(Team.Black), 4, 7);

        Assert.Throws<MoveLeavesKingInCheckException>(() =>
            board.MakeMove(new PieceMove(new PieceCord(4, 3), new PieceCord(3, 2)), Team.White));
    }

    [Fact]
    public void MakeMove_BlockingPieceStopsCheck_IsLegal()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.White), 0, 3);
        board.Place(new Rook(Team.Black), 4, 7);

        board.MakeMove(new PieceMove(new PieceCord(0, 3), new PieceCord(4, 3)), Team.White);

        Assert.False(board.IsInCheck(Team.White));
        Assert.IsType<Rook>(board.Spots[4][3].Piece);
    }
}
