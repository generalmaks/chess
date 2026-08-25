using Chess.Model.Chess.Game;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Game;

public class CheckTimeoutTests
{
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
}
