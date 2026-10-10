using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Game;

public class MakeMoveTests
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
        session.Board.Place(new King(Team.Black), 0, 7);
        session.Board.Place(new Rook(Team.White), 1, 3);
        session.Board.Place(new Rook(Team.Black), 1, 4);

        // Rook tours of length 7 and 13 never revisit a position within 100 plies,
        // so threefold repetition can't end the game first.
        PieceCord[] whiteTour = [.. Enumerable.Range(1, 7).Select(x => new PieceCord(x, 3))];
        PieceCord[] blackTour =
        [
            .. Enumerable.Range(1, 7).Select(x => new PieceCord(x, 4)),
            .. new[] { 7, 6, 5, 4, 3, 1 }.Select(x => new PieceCord(x, 5)),
        ];

        for (var i = 0; i < 50; i++)
        {
            session.MakeMove(new PieceMove(whiteTour[i % 7], whiteTour[(i + 1) % 7]));
            session.MakeMove(new PieceMove(blackTour[i % 13], blackTour[(i + 1) % 13]));
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
    public void MakeMove_DoesNotAutomaticallyCheckTimeout()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);

        time.Advance(TimeSpan.FromMinutes(10));
        session.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)));

        Assert.Equal(GameResult.Ongoing, session.Result);
    }
}
