using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Api.Contracts;

public static class GameStateMapper
{
    public static GameStateDto ToDto(ChessGameSession session)
    {
        var board = new string?[8][];
        for (var x = 0; x < 8; x++)
        {
            board[x] = new string?[8];
            for (var y = 0; y < 8; y++)
            {
                var piece = session.Board.Spots[x][y].Piece;
                board[x][y] = piece is null ? null : $"{(piece.Team == Team.White ? 'w' : 'b')}{piece.PieceCode}";
            }
        }

        return new GameStateDto(board, session.CurrentTurn.ToString(), session.Result.ToString());
    }
}
