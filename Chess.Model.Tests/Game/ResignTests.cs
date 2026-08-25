using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Tests.Game;

public class ResignTests
{
    [Fact]
    public void Resign_WhiteResigns_BlackWinsAndBlocksFurtherMoves()
    {
        var session = new ChessGameSession();

        session.Resign(Team.White);

        Assert.Equal(GameResult.BlackWonByResignation, session.Result);
        Assert.Throws<GameAlreadyEndedException>(() =>
            session.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 5))));
    }

    [Fact]
    public void Resign_BlackResigns_WhiteWins()
    {
        var session = new ChessGameSession();

        session.Resign(Team.Black);

        Assert.Equal(GameResult.WhiteWonByResignation, session.Result);
    }

    [Fact]
    public void Resign_GameAlreadyEnded_ThrowsGameAlreadyEndedException()
    {
        var session = new ChessGameSession();
        session.Resign(Team.White);

        Assert.Throws<GameAlreadyEndedException>(() => session.Resign(Team.Black));
    }
}
