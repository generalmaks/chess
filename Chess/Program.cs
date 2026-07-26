// See https://aka.ms/new-console-template for more information

using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

var board = new ChessBoard();
Console.BackgroundColor = ConsoleColor.Gray;
for (int i = 0; i < board.Spots.Length; i++)
{
    var rank =  board.Spots[i].Length;
    for (int j = 0; j < rank; j++)
    {
        var piece = board.Spots[j][i].Piece;
        if (piece != null)
        {
            Console.ForegroundColor = piece.Team == Team.White ? ConsoleColor.White : ConsoleColor.Black;
            Console.Write(piece.PieceCode);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("x");
        }
    }
    Console.WriteLine("");
}