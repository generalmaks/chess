using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Game;

public class TimeRemainingTests
{
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
}
