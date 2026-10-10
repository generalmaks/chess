using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Game;

public class DrawRulesTests
{
    [Fact]
    public void MakeMove_SamePositionOccursThreeTimes_SetsResultToThreefoldRepetitionDraw()
    {
        var session = new ChessGameSession();
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 0, 0);
        session.Board.Place(new King(Team.Black), 7, 7);
        session.Board.Place(new Knight(Team.White), 1, 0);
        session.Board.Place(new Knight(Team.Black), 6, 7);

        // The starting position recurs after plies 4 and 8.
        for (var ply = 1; ply <= 8; ply++)
        {
            Assert.Equal(GameResult.Ongoing, session.Result);
            ShuffleKnights(session);
        }

        Assert.Equal(GameResult.ThreefoldRepetitionDraw, session.Result);
    }

    [Fact]
    public void MakeMove_CaptureLeavesKingAndBishopVersusKing_SetsResultToInsufficientMaterialDraw()
    {
        var session = new ChessGameSession();
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 0, 7);
        session.Board.Place(new Bishop(Team.White), 2, 2);
        session.Board.Place(new King(Team.Black), 7, 0);
        session.Board.Place(new Knight(Team.Black), 4, 4);

        session.MakeMove(new PieceMove(new PieceCord(2, 2), new PieceCord(4, 4)));

        Assert.Equal(GameResult.InsufficientMaterialDraw, session.Result);
    }

    [Fact]
    public void CheckTimeout_OpponentHasOnlyKing_SetsResultToTimeoutInsufficientMaterialDraw()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 0, 0);
        session.Board.Place(new Queen(Team.White), 3, 3);
        session.Board.Place(new King(Team.Black), 7, 7);
        session.StartClock();

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.True(session.CheckTimeout());
        Assert.Equal(GameResult.TimeoutInsufficientMaterialDraw, session.Result);
    }

    [Fact]
    public void CheckTimeout_OpponentHasKnightAgainstQueen_OpponentWinsOnTime()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var session = new ChessGameSession(TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero), time);
        session.Board.Clear();
        session.Board.Place(new King(Team.White), 0, 0);
        session.Board.Place(new Queen(Team.White), 3, 3);
        session.Board.Place(new King(Team.Black), 7, 7);
        session.Board.Place(new Knight(Team.Black), 6, 7);
        session.StartClock();

        time.Advance(TimeSpan.FromMinutes(6));

        Assert.True(session.CheckTimeout());
        Assert.Equal(GameResult.BlackWonOnTime, session.Result);
    }

    private static void ShuffleKnights(ChessGameSession session)
    {
        var (home, away) = session.CurrentTurn == Team.White
            ? (new PieceCord(1, 0), new PieceCord(2, 2))
            : (new PieceCord(6, 7), new PieceCord(5, 5));
        var from = session.Board.Spots[home.X][home.Y].Piece is Knight ? home : away;
        session.MakeMove(new PieceMove(from, from == home ? away : home));
    }
}
