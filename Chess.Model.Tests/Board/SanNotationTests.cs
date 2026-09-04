using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class SanNotationTests
{
    private static string San(ChessBoard board) => board.MoveHistory[^1].San;

    [Fact]
    public void PlainPawnPush()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 0, 1);

        board.MakeMove(new PieceMove(new PieceCord(0, 1), new PieceCord(0, 3)), Team.White);

        Assert.Equal("a4", San(board));
    }

    [Fact]
    public void PawnCapture()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 4, 3);
        board.Place(new Pawn(Team.Black), 3, 4);

        board.MakeMove(new PieceMove(new PieceCord(4, 3), new PieceCord(3, 4)), Team.White);

        Assert.Equal("exd5", San(board));
    }

    [Fact]
    public void PieceMove()
    {
        var board = TestBoard.Empty();
        board.Place(new Knight(Team.White), 1, 0);

        board.MakeMove(new PieceMove(new PieceCord(1, 0), new PieceCord(2, 2)), Team.White);

        Assert.Equal("Nc3", San(board));
    }

    [Fact]
    public void PieceCapture()
    {
        var board = TestBoard.Empty();
        board.Place(new Bishop(Team.White), 2, 0);
        board.Place(new Pawn(Team.Black), 6, 4);

        board.MakeMove(new PieceMove(new PieceCord(2, 0), new PieceCord(6, 4)), Team.White);

        Assert.Equal("Bxg5", San(board));
    }

    [Fact]
    public void DisambiguatesByFile()
    {
        var board = TestBoard.Empty();
        board.Place(new Knight(Team.White), 1, 0);
        board.Place(new Knight(Team.White), 3, 4);

        board.MakeMove(new PieceMove(new PieceCord(1, 0), new PieceCord(2, 2)), Team.White);

        Assert.Equal("Nbc3", San(board));
    }

    [Fact]
    public void DisambiguatesByRankWhenFileShared()
    {
        var board = TestBoard.Empty();
        board.Place(new Rook(Team.White), 0, 0);
        board.Place(new Rook(Team.White), 0, 4);

        board.MakeMove(new PieceMove(new PieceCord(0, 0), new PieceCord(0, 2)), Team.White);

        Assert.Equal("R1a3", San(board));
    }

    [Fact]
    public void KingsideCastling()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.White), 7, 0);

        board.MakeMove(new PieceMove(new PieceCord(4, 0), new PieceCord(6, 0)), Team.White);

        Assert.Equal("O-O", San(board));
    }

    [Fact]
    public void QueensideCastling()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 4, 0);
        board.Place(new Rook(Team.White), 0, 0);

        board.MakeMove(new PieceMove(new PieceCord(4, 0), new PieceCord(2, 0)), Team.White);

        Assert.Equal("O-O-O", San(board));
    }

    [Fact]
    public void Promotion()
    {
        var board = TestBoard.Empty();
        board.Place(new Pawn(Team.White), 0, 6);

        board.MakeMove(new PieceMove(new PieceCord(0, 6), new PieceCord(0, 7)), Team.White, 'Q');

        Assert.Equal("a8=Q", San(board));
    }

    [Fact]
    public void Check()
    {
        var board = TestBoard.Empty();
        board.Place(new Queen(Team.White), 0, 0);
        board.Place(new King(Team.Black), 7, 7);

        board.MakeMove(new PieceMove(new PieceCord(0, 0), new PieceCord(0, 7)), Team.White);

        Assert.Equal("Qa8+", San(board));
    }

    [Fact]
    public void Checkmate()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.Black), 7, 7);
        board.Place(new Pawn(Team.Black), 6, 6);
        board.Place(new Pawn(Team.Black), 7, 6);
        board.Place(new Rook(Team.White), 0, 0);

        board.MakeMove(new PieceMove(new PieceCord(0, 0), new PieceCord(0, 7)), Team.White);

        Assert.Equal("Ra8#", San(board));
    }

    [Fact]
    public void EnPassantCapture()
    {
        var board = new ChessBoard();

        board.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), Team.White);
        board.MakeMove(new PieceMove(new PieceCord(0, 6), new PieceCord(0, 5)), Team.Black);
        board.MakeMove(new PieceMove(new PieceCord(4, 3), new PieceCord(4, 4)), Team.White);
        board.MakeMove(new PieceMove(new PieceCord(3, 6), new PieceCord(3, 4)), Team.Black);
        board.MakeMove(new PieceMove(new PieceCord(4, 4), new PieceCord(3, 5)), Team.White);

        Assert.Equal("exd6", San(board));
    }
}
