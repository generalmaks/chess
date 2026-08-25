using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Tests.Game;

public class AbandonTests
{
    [Fact]
    public void Abandon_WhiteAbandons_BlackWinsAndBlocksFurtherMoves()
    {
        var session = new ChessGameSession();

        session.Abandon(Team.White);

        Assert.Equal(GameResult.BlackWonByAbandonment, session.Result);
        Assert.Throws<GameAlreadyEndedException>(() =>
            session.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 5))));
    }

    [Fact]
    public void Abandon_BlackAbandons_WhiteWins()
    {
        var session = new ChessGameSession();

        session.Abandon(Team.Black);

        Assert.Equal(GameResult.WhiteWonByAbandonment, session.Result);
    }

    [Fact]
    public void Abandon_GameAlreadyEnded_ThrowsGameAlreadyEndedException()
    {
        var session = new ChessGameSession();
        session.Abandon(Team.White);

        Assert.Throws<GameAlreadyEndedException>(() => session.Abandon(Team.Black));
    }
}
