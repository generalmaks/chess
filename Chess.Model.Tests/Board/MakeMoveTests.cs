using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class MakeMoveTests
{
    [Fact]
    public void MakeMove_LegalMove_MovesPieceToDestinationAndClearsOrigin()
    {
        var board = new ChessBoard();
        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3));

        board.MakeMove(move, Team.White);

        Assert.False(board.Spots[4][1].IsSpotOccupied);
        var movedPiece = Assert.IsType<Pawn>(board.Spots[4][3].Piece);
        Assert.Equal(Team.White, movedPiece.Team);
        Assert.True(movedPiece.HasMadeFirstMove);
    }

    [Fact]
    public void MakeMove_FromEmptySpot_ThrowsNonExistingPieceException()
    {
        var board = new ChessBoard();
        var move = new PieceMove(new PieceCord(4, 4), new PieceCord(4, 5));

        Assert.Throws<NonExistingPieceException>(() => board.MakeMove(move, Team.White));
    }

    [Fact]
    public void MakeMove_EnemyPiece_ThrowsEnemyPieceMovingException()
    {
        var board = new ChessBoard();
        var move = new PieceMove(new PieceCord(4, 6), new PieceCord(4, 5));

        Assert.Throws<EnemyPieceMovingException>(() => board.MakeMove(move, Team.White));
    }

    [Fact]
    public void MakeMove_OntoOwnPiece_ThrowsMovingOverOwnPiecesException()
    {
        var board = TestBoard.Empty();
        board.Place(new Rook(Team.White), 0, 0);
        board.Place(new Pawn(Team.White), 0, 3);
        var move = new PieceMove(new PieceCord(0, 0), new PieceCord(0, 3));

        Assert.Throws<MovingOverOwnPiecesException>(() => board.MakeMove(move, Team.White));
    }

    [Fact]
    public void MakeMove_IllegalMove_ThrowsNoPossibleMovesException()
    {
        var board = new ChessBoard();
        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 4));

        Assert.Throws<NoPossibleMovesException>(() => board.MakeMove(move, Team.White));
    }

    [Fact]
    public void MakeMove_PawnFirstMove_LimitsFollowUpMoveToOneSquare()
    {
        var board = new ChessBoard();
        board.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 2)), Team.White);

        var pawn = Assert.IsType<Pawn>(board.Spots[4][2].Piece);
        var moves = pawn.PossibleMoves(board, new PieceCord(4, 2));

        Assert.Equivalent(new[] { new PieceCord(4, 3) }, moves.Select(m => m.To).ToArray());
    }

    private static ChessBoard SetupCastlingBoard(out Rook kingSideRook, out Rook queenSideRook)
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        kingSideRook = new Rook(Team.White);
        queenSideRook = new Rook(Team.White);
        board.Place(king, 4, 0);
        board.Place(kingSideRook, 7, 0);
        board.Place(queenSideRook, 0, 0);
        return board;
    }

    [Fact]
    public void MakeMove_KingsideCastle_MovesKingAndRook()
    {
        var board = SetupCastlingBoard(out var rook, out _);

        board.MakeMove(new PieceMove(new PieceCord(4, 0), new PieceCord(6, 0)), Team.White);

        Assert.IsType<King>(board.Spots[6][0].Piece);
        Assert.False(board.Spots[4][0].IsSpotOccupied);
        Assert.Same(rook, board.Spots[5][0].Piece);
        Assert.False(board.Spots[7][0].IsSpotOccupied);
        Assert.True(rook.HasMoved);
    }

    [Fact]
    public void MakeMove_QueensideCastle_MovesKingAndRook()
    {
        var board = SetupCastlingBoard(out _, out var rook);

        board.MakeMove(new PieceMove(new PieceCord(4, 0), new PieceCord(2, 0)), Team.White);

        Assert.IsType<King>(board.Spots[2][0].Piece);
        Assert.False(board.Spots[4][0].IsSpotOccupied);
        Assert.Same(rook, board.Spots[3][0].Piece);
        Assert.False(board.Spots[0][0].IsSpotOccupied);
        Assert.True(rook.HasMoved);
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
