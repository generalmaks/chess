using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests.Board;

public class PawnPromotionTests
{
    [Fact]
    public void MakeMove_WhitePawnReachesLastRankWithNoChoice_DefaultsToQueen()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 0, 6);

        board.MakeMove(new PieceMove(new PieceCord(0, 6), new PieceCord(0, 7)), Team.White);

        var promoted = Assert.IsType<Queen>(board.Spots[0][7].Piece);
        Assert.Equal(Team.White, promoted.Team);
        Assert.False(board.Spots[0][6].IsSpotOccupied);
    }

    [Fact]
    public void MakeMove_BlackPawnReachesLastRankWithNoChoice_DefaultsToQueen()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.Black), 0, 1);

        board.MakeMove(new PieceMove(new PieceCord(0, 1), new PieceCord(0, 0)), Team.Black);

        var promoted = Assert.IsType<Queen>(board.Spots[0][0].Piece);
        Assert.Equal(Team.Black, promoted.Team);
    }

    [Theory]
    [InlineData('R', typeof(Rook))]
    [InlineData('B', typeof(Bishop))]
    [InlineData('N', typeof(Knight))]
    [InlineData('Q', typeof(Queen))]
    public void MakeMove_PawnReachesLastRankWithChoice_PromotesToChosenPiece(char choice, Type expectedType)
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 0, 6);

        board.MakeMove(new PieceMove(new PieceCord(0, 6), new PieceCord(0, 7)), Team.White, choice);

        Assert.IsType(expectedType, board.Spots[0][7].Piece);
    }

    [Fact]
    public void MakeMove_PawnReachesLastRankWithInvalidChoice_ThrowsInvalidPromotionPieceException()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 0, 6);

        Assert.Throws<InvalidPromotionPieceException>(() =>
            board.MakeMove(new PieceMove(new PieceCord(0, 6), new PieceCord(0, 7)), Team.White, 'K'));
    }

    [Fact]
    public void MakeMove_PawnNotReachingLastRank_RemainsPawn()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 0, 5);

        board.MakeMove(new PieceMove(new PieceCord(0, 5), new PieceCord(0, 6)), Team.White);

        Assert.IsType<Pawn>(board.Spots[0][6].Piece);
    }

    [Fact]
    public void MakeMove_PawnPromotion_RecordsPromotedToInHistory()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 0, 6);

        board.MakeMove(new PieceMove(new PieceCord(0, 6), new PieceCord(0, 7)), Team.White, 'R');

        Assert.Equal('R', board.MoveHistory[^1].PromotedTo);
    }
}
