using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Orchestrator.Tests.Game;

public class CheckTimeoutAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task CheckTimeoutAsync_ClockExpired_EndsGameOnTimeAndPersists()
    {
        var (orchestrator, repo, time) = CreateOrchestratorWithClock();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White, TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero));
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        time.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        var endedRoom = await orchestrator.CheckTimeoutAsync(room.Id);

        Assert.NotNull(endedRoom);
        Assert.Equal(GameResult.BlackWonOnTime, endedRoom!.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWonOnTime, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);
    }

    [Fact]
    public async Task CheckTimeoutAsync_ClockNotExpired_ReturnsNullAndLeavesGameOngoing()
    {
        var (orchestrator, _, time) = CreateOrchestratorWithClock();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White, TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero));
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        time.Advance(TimeSpan.FromMinutes(1));
        var endedRoom = await orchestrator.CheckTimeoutAsync(room.Id);

        Assert.Null(endedRoom);
        Assert.Equal(GameResult.Ongoing, room.State.Result);
    }

    [Fact]
    public async Task CheckTimeoutAsync_BeforeOpponentJoins_DoesNotEndGame()
    {
        var (orchestrator, _, time) = CreateOrchestratorWithClock();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White, TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero));
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        time.Advance(TimeSpan.FromMinutes(10));
        var endedRoom = await orchestrator.CheckTimeoutAsync(room.Id);

        Assert.Null(endedRoom);
        Assert.Equal(GameResult.Ongoing, room.State.Result);
    }

    [Fact]
    public async Task CheckTimeoutAsync_UnknownGameId_ReturnsNull()
    {
        var (orchestrator, _, _) = CreateOrchestratorWithClock();

        var endedRoom = await orchestrator.CheckTimeoutAsync("missing-game");

        Assert.Null(endedRoom);
    }
}
