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

        Assert.Equal(GameResult.WhiteWonByCheckmate, session.Result);
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

    [Fact]
    public void AgreeToDraw_GameAlreadyEnded_ThrowsGameAlreadyEndedException()
    {
        var session = new ChessGameSession();
        session.AgreeToDraw();

        Assert.Throws<GameAlreadyEndedException>(session.AgreeToDraw);
    }

    [Fact]
    public void TimeRemaining_UntimedGame_ReturnsNull()
    {
        var session = new ChessGameSession();

        Assert.Null(session.TimeRemaining(Team.White));
        Assert.Null(session.TimeRemaining(Team.Black));
    }

    [Fact]
    public void TimeRemaining_TeamOnMove_ReflectsElapsedTime()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);
        session.StartClock();

        time.Advance(TimeSpan.FromSeconds(20));

        Assert.Equal(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(20), session.TimeRemaining(Team.White));
        Assert.Equal(TimeSpan.FromMinutes(5), session.TimeRemaining(Team.Black));
    }

    [Fact]
    public void TimeRemaining_ClockNotStarted_DoesNotElapse()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);

        time.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromMinutes(5), session.TimeRemaining(Team.White));
        Assert.Equal(TimeSpan.FromMinutes(5), session.TimeRemaining(Team.Black));
    }

    [Fact]
    public void MakeMove_DeductsSpentTimeAndAppliesIncrement()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2)), time);
        session.StartClock();

        time.Advance(TimeSpan.FromSeconds(10));
        session.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)));

        Assert.Equal(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(8), session.TimeRemaining(Team.White));
        Assert.Equal(TimeSpan.FromMinutes(5), session.TimeRemaining(Team.Black));
    }

    [Fact]
    public void CheckTimeout_ClockExpired_SetsResultAndReturnsTrue()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);
        session.StartClock();

        time.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        Assert.True(session.CheckTimeout());
        Assert.Equal(GameResult.BlackWonOnTime, session.Result);
    }

    [Fact]
    public void CheckTimeout_ClockNotExpired_ReturnsFalseAndLeavesGameOngoing()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);
        session.StartClock();

        time.Advance(TimeSpan.FromMinutes(1));

        Assert.False(session.CheckTimeout());
        Assert.Equal(GameResult.Ongoing, session.Result);
    }

    [Fact]
    public void CheckTimeout_ClockNotStarted_ReturnsFalseEvenAfterFullDurationElapsed()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);

        time.Advance(TimeSpan.FromMinutes(10));

        Assert.False(session.CheckTimeout());
        Assert.Equal(GameResult.Ongoing, session.Result);
    }

    [Fact]
    public void CheckTimeout_UntimedGame_ReturnsFalse()
    {
        var session = new ChessGameSession();

        Assert.False(session.CheckTimeout());
    }

    [Fact]
    public void StartClock_ResetsElapsedTimeForCurrentMover()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);

        time.Advance(TimeSpan.FromMinutes(10));
        session.StartClock();

        Assert.False(session.CheckTimeout());
        Assert.Equal(TimeSpan.FromMinutes(5), session.TimeRemaining(Team.White));
    }

    [Fact]
    public void MakeMove_DoesNotAutomaticallyCheckTimeout()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);

        time.Advance(TimeSpan.FromMinutes(10));
        session.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)));

        Assert.Equal(GameResult.Ongoing, session.Result);
    }
}
