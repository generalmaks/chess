using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests;

public class ChessBoardMakeMoveTests
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
    public void MakeMove_FromEmptySpot_ThrowsArgumentException()
    {
        var board = new ChessBoard();
        var move = new PieceMove(new PieceCord(4, 4), new PieceCord(4, 5));

        Assert.Throws<ArgumentException>(() => board.MakeMove(move, Team.White));
    }

    [Fact]
    public void MakeMove_EnemyPiece_ThrowsArgumentException()
    {
        var board = new ChessBoard();
        var move = new PieceMove(new PieceCord(4, 6), new PieceCord(4, 5));

        Assert.Throws<ArgumentException>(() => board.MakeMove(move, Team.White));
    }

    [Fact]
    public void MakeMove_OntoOwnPiece_ThrowsArgumentException()
    {
        var board = TestBoard.Empty();
        board.Place(new Rook(Team.White), 0, 0);
        board.Place(new Pawn(Team.White), 0, 3);
        var move = new PieceMove(new PieceCord(0, 0), new PieceCord(0, 3));

        Assert.Throws<ArgumentException>(() => board.MakeMove(move, Team.White));
    }

    [Fact]
    public void MakeMove_IllegalMove_ThrowsArgumentException()
    {
        var board = new ChessBoard();
        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 4));

        Assert.Throws<ArgumentException>(() => board.MakeMove(move, Team.White));
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
}
