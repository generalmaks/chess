using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;

namespace Chess.Model.Tests.Game;

public class AgreeToDrawTests
{
    [Fact]
    public void AgreeToDraw_SetsResultToDrawByAgreementAndBlocksFurtherMoves()
    {
        var session = new ChessGameSession();

        session.AgreeToDraw();

        Assert.Equal(GameResult.DrawByAgreement, session.Result);
        Assert.Throws<GameAlreadyEndedException>(() =>
            session.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3))));
    }

    [Fact]
    public void AgreeToDraw_GameAlreadyEnded_ThrowsGameAlreadyEndedException()
    {
        var session = new ChessGameSession();
        session.AgreeToDraw();

        Assert.Throws<GameAlreadyEndedException>(session.AgreeToDraw);
    }
}
