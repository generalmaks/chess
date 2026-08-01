using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests.Board;

public class CastlingTests
{
    private static ChessBoard SetupBoard(out King king, out Rook kingSideRook, out Rook queenSideRook)
    {
        var board = TestBoard.Empty();
        king = new King(Team.White);
        kingSideRook = new Rook(Team.White);
        queenSideRook = new Rook(Team.White);
        board.Place(king, 4, 0);
        board.Place(kingSideRook, 7, 0);
        board.Place(queenSideRook, 0, 0);
        return board;
    }

    [Fact]
    public void PossibleMoves_ClearPathBothSides_IncludesBothCastlingMoves()
    {
        var board = SetupBoard(out var king, out _, out _);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.Contains(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void MakeMove_KingsideCastle_MovesKingAndRook()
    {
        var board = SetupBoard(out _, out var rook, out _);

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
        var board = SetupBoard(out _, out _, out var rook);

        board.MakeMove(new PieceMove(new PieceCord(4, 0), new PieceCord(2, 0)), Team.White);

        Assert.IsType<King>(board.Spots[2][0].Piece);
        Assert.False(board.Spots[4][0].IsSpotOccupied);
        Assert.Same(rook, board.Spots[3][0].Piece);
        Assert.False(board.Spots[0][0].IsSpotOccupied);
        Assert.True(rook.HasMoved);
    }

    [Fact]
    public void PossibleMoves_PieceBetweenKingAndRook_ExcludesThatSideCastle()
    {
        var board = SetupBoard(out var king, out _, out _);
        board.Place(new Bishop(Team.White), 5, 0);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_KingAlreadyMoved_ExcludesCastling()
    {
        var board = SetupBoard(out var king, out _, out _);
        king.HasMoved = true;

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_RookAlreadyMoved_ExcludesThatSideCastle()
    {
        var board = SetupBoard(out var king, out var kingSideRook, out _);
        kingSideRook.HasMoved = true;

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_KingInCheck_ExcludesCastling()
    {
        var board = SetupBoard(out var king, out _, out _);
        board.Place(new Rook(Team.Black), 4, 7);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_SquareKingPassesThroughIsAttacked_ExcludesThatSideCastle()
    {
        var board = SetupBoard(out var king, out _, out _);
        board.Place(new Rook(Team.Black), 5, 7);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_LandingSquareIsAttacked_ExcludesThatSideCastle()
    {
        var board = SetupBoard(out var king, out _, out _);
        board.Place(new Rook(Team.Black), 2, 7);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(2, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(6, 0));
    }
}
