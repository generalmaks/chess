using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Logic.Tests.Support;

namespace Chess.Logic.Tests;

public class ChessGameSessionTests
{
    [Fact]
    public void MakeMove_CheckmateDelivered_SetsResultAndBlocksFurtherMoves()
    {
        var session = new ChessGameSession();
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 4, 0);
        session.Board.Place(new King(Team.Black), 4, 7);
        session.Board.Place(new Pawn(Team.Black), 3, 6);
        session.Board.Place(new Pawn(Team.Black), 4, 6);
        session.Board.Place(new Pawn(Team.Black), 5, 6);
        session.Board.Place(new Rook(Team.White), 0, 0);

        session.MakeMove(new PieceMove(new PieceCord(0, 0), new PieceCord(0, 7)));

        Assert.Equal(GameResult.WhiteWon, session.Result);
        Assert.Throws<GameAlreadyEndedException>(() =>
            session.MakeMove(new PieceMove(new PieceCord(4, 0), new PieceCord(4, 1))));
    }

    [Fact]
    public void MakeMove_StalemateDelivered_SetsResultToStalemateDraw()
    {
        var session = new ChessGameSession();
        session.Board.Clear();
        session.Board.Place(new King(Team.Black), 0, 7);
        session.Board.Place(new King(Team.White), 2, 6);
        session.Board.Place(new Queen(Team.White), 1, 0);

        session.MakeMove(new PieceMove(new PieceCord(1, 0), new PieceCord(1, 5)));

        Assert.Equal(GameResult.StalemateDraw, session.Result);
    }

    [Fact]
    public void MakeMove_FiftyMoveRuleReached_SetsResultToFiftyMoveRuleDraw()
    {
        var session = new ChessGameSession();
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 0, 0);
        session.Board.Place(new King(Team.Black), 7, 7);
        session.Board.Place(new Knight(Team.White), 1, 0);
        session.Board.Place(new Knight(Team.Black), 6, 7);

        var whiteAt = new PieceCord(1, 0);
        var whiteAlt = new PieceCord(2, 2);
        var blackAt = new PieceCord(6, 7);
        var blackAlt = new PieceCord(5, 5);

        for (var i = 0; i < 100; i++)
        {
            if (session.CurrentTurn == Team.White)
            {
                session.MakeMove(new PieceMove(whiteAt, whiteAlt));
                (whiteAt, whiteAlt) = (whiteAlt, whiteAt);
            }
            else
            {
                session.MakeMove(new PieceMove(blackAt, blackAlt));
                (blackAt, blackAlt) = (blackAlt, blackAt);
            }
        }

        Assert.Equal(100, session.HalfmoveClock);
        Assert.Equal(GameResult.FiftyMoveRuleDraw, session.Result);
    }

    [Fact]
    public void MakeMove_Capture_RecordsCapturedPieceAndResetsHalfmoveClock()
    {
        var session = new ChessGameSession();
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 0, 0);
        session.Board.Place(new King(Team.Black), 7, 7);
        session.Board.Place(new Rook(Team.White), 0, 3);
        session.Board.Place(new Knight(Team.Black), 0, 6);

        session.MakeMove(new PieceMove(new PieceCord(0, 0), new PieceCord(0, 1)));
        session.MakeMove(new PieceMove(new PieceCord(7, 7), new PieceCord(6, 7)));
        session.MakeMove(new PieceMove(new PieceCord(0, 3), new PieceCord(0, 6)));

        Assert.Equal(0, session.HalfmoveClock);
        var captured = Assert.Single(session.PiecesCapturedByWhite);
        Assert.IsType<Knight>(captured);
        Assert.Empty(session.PiecesCapturedByBlack);
    }

    [Fact]
    public void MakeMove_EnPassantCapture_RecordsCapturedPawnAndResetsHalfmoveClock()
    {
        var session = new ChessGameSession();
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 0, 0);
        session.Board.Place(new King(Team.Black), 7, 7);
        session.Board.Place(new Knight(Team.White), 1, 0);
        session.Board.Place(new Pawn(Team.White) { HasMadeFirstMove = true }, 3, 4);
        session.Board.Place(new Pawn(Team.Black), 4, 6);

        session.MakeMove(new PieceMove(new PieceCord(1, 0), new PieceCord(2, 2)));
        session.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)));
        session.MakeMove(new PieceMove(new PieceCord(3, 4), new PieceCord(4, 5)));

        Assert.Equal(0, session.HalfmoveClock);
        var captured = Assert.Single(session.PiecesCapturedByWhite);
        Assert.IsType<Pawn>(captured);
        Assert.False(session.Board.Spots[4][4].IsSpotOccupied);
        Assert.Empty(session.PiecesCapturedByBlack);
    }

    [Fact]
    public void Resign_WhiteResigns_BlackWinsAndBlocksFurtherMoves()
    {
        var session = new ChessGameSession();

        session.Resign(Team.White);

        Assert.Equal(GameResult.BlackWon, session.Result);
        Assert.Throws<GameAlreadyEndedException>(() =>
            session.MakeMove(new PieceMove(new PieceCord(4, 6), new PieceCord(4, 5))));
    }

    [Fact]
    public void Resign_BlackResigns_WhiteWins()
    {
        var session = new ChessGameSession();

        session.Resign(Team.Black);

        Assert.Equal(GameResult.WhiteWon, session.Result);
    }

    [Fact]
    public void Resign_GameAlreadyEnded_ThrowsGameAlreadyEndedException()
    {
        var session = new ChessGameSession();
        session.Resign(Team.White);

        Assert.Throws<GameAlreadyEndedException>(() => session.Resign(Team.Black));
    }

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
