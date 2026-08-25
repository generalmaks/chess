using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Chess.Game;

public sealed record GameStateSnapshot(
    string?[][] Board,
    Team CurrentTurn,
    GameResult Result,
    TimeSpan? WhiteTimeRemaining,
    TimeSpan? BlackTimeRemaining)
{
    public static GameStateSnapshot Capture(ChessGameSession session)
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

        return new GameStateSnapshot(
            board,
            session.CurrentTurn,
            session.Result,
            session.TimeRemaining(Team.White),
            session.TimeRemaining(Team.Black));
    }
}
