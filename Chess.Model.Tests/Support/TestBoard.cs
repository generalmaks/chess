using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Tests.Support;

internal static class TestBoard
{
    public static ChessBoard Empty()
    {
        var board = new ChessBoard();
        board.Clear();
        return board;
    }

    public static void Clear(this ChessBoard board)
    {
        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 8; y++)
            {
                board.Spots[x][y].SetPiece(null);
            }
        }
    }

    public static void Place(this ChessBoard board, Piece piece, int x, int y) =>
        board.Spots[x][y].SetPiece(piece);
}
