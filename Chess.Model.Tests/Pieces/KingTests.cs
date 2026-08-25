using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Pieces;

public class KingTests
{
    [Fact]
    public void PossibleMoves_CenterOfEmptyBoard_ReturnsAllEightMoves()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(king, coord.X, coord.Y);

        var moves = king.PossibleMoves(board, coord);

        Assert.Equal(8, moves.Length);
    }

    [Fact]
    public void PossibleMoves_Corner_ReturnsOnlyThreeMoves()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(0, 0);
        board.Place(king, coord.X, coord.Y);

        var moves = king.PossibleMoves(board, coord);

        Assert.Equivalent(new[]
        {
            new PieceCord(1, 0), new PieceCord(0, 1), new PieceCord(1, 1)
        }, moves.Select(m => m.To).ToArray());
    }

    [Fact]
    public void PossibleMoves_FriendlyPieceOnTarget_ExcludesThatSquare()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(king, coord.X, coord.Y);
        board.Place(new Pawn(Team.White), 4, 5);

        var moves = king.PossibleMoves(board, coord);

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(4, 5));
        Assert.Equal(7, moves.Length);
    }

    [Fact]
    public void PossibleMoves_EnemyPieceOnTarget_IncludesCapture()
    {
        var board = TestBoard.Empty();
        var king = new King(Team.White);
        var coord = new PieceCord(4, 4);
        board.Place(king, coord.X, coord.Y);
        board.Place(new Pawn(Team.Black), 4, 5);

        var moves = king.PossibleMoves(board, coord);

        Assert.Contains(moves, m => m.To == new PieceCord(4, 5));
        Assert.Equal(8, moves.Length);
    }

    private static ChessBoard SetupCastlingBoard(out King king, out Rook kingSideRook, out Rook queenSideRook)
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
        var board = SetupCastlingBoard(out var king, out _, out _);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.Contains(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_PieceBetweenKingAndRook_ExcludesThatSideCastle()
    {
        var board = SetupCastlingBoard(out var king, out _, out _);
        board.Place(new Bishop(Team.White), 5, 0);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_KingAlreadyMoved_ExcludesCastling()
    {
        var board = SetupCastlingBoard(out var king, out _, out _);
        king.HasMoved = true;

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_RookAlreadyMoved_ExcludesThatSideCastle()
    {
        var board = SetupCastlingBoard(out var king, out var kingSideRook, out _);
        kingSideRook.HasMoved = true;

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_KingInCheck_ExcludesCastling()
    {
        var board = SetupCastlingBoard(out var king, out _, out _);
        board.Place(new Rook(Team.Black), 4, 7);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.DoesNotContain(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_SquareKingPassesThroughIsAttacked_ExcludesThatSideCastle()
    {
        var board = SetupCastlingBoard(out var king, out _, out _);
        board.Place(new Rook(Team.Black), 5, 7);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(6, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(2, 0));
    }

    [Fact]
    public void PossibleMoves_LandingSquareIsAttacked_ExcludesThatSideCastle()
    {
        var board = SetupCastlingBoard(out var king, out _, out _);
        board.Place(new Rook(Team.Black), 2, 7);

        var moves = king.PossibleMoves(board, new PieceCord(4, 0));

        Assert.DoesNotContain(moves, m => m.To == new PieceCord(2, 0));
        Assert.Contains(moves, m => m.To == new PieceCord(6, 0));
    }
}
