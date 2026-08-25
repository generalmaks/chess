using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Game;

public class StartClockTests
{
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
}
