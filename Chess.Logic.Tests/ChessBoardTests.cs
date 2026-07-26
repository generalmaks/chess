using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Logic.Tests;

public class ChessBoardTests
{
    [Fact]
    public void ChessBoard_CreateBoard_PutsAllPiecesInCorrectPlace()
    {
        var board = new ChessBoard();

        AssertBackRank(board, 0, Team.White);
        AssertBackRank(board, 7, Team.Black);
        AssertPawnRank(board, 1, Team.White);
        AssertPawnRank(board, 6, Team.Black);
        AssertEmptyRanks(board, 2, 5);
    }

    private static void AssertBackRank(ChessBoard board, int y, Team team)
    {
        AssertPiece<Rook>(board, 0, y, team);
        AssertPiece<Knight>(board, 1, y, team);
        AssertPiece<Bishop>(board, 2, y, team);
        AssertPiece<Queen>(board, 3, y, team);
        AssertPiece<King>(board, 4, y, team);
        AssertPiece<Bishop>(board, 5, y, team);
        AssertPiece<Knight>(board, 6, y, team);
        AssertPiece<Rook>(board, 7, y, team);
    }

    private static void AssertPawnRank(ChessBoard board, int y, Team team)
    {
        for (int x = 0; x < 8; x++)
        {
            AssertPiece<Pawn>(board, x, y, team);
        }
    }

    private static void AssertEmptyRanks(ChessBoard board, int fromY, int toY)
    {
        for (int y = fromY; y <= toY; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Assert.False(board.Spots[x][y].IsSpotOccupied);
            }
        }
    }

    private static void AssertPiece<TPiece>(ChessBoard board, int x, int y, Team team) where TPiece : Piece
    {
        var piece = board.Spots[x][y].Piece;
        var typedPiece = Assert.IsType<TPiece>(piece);
        Assert.Equal(team, typedPiece.Team);
    }
}