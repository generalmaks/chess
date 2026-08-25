using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;

namespace Chess.Orchestrator.Tests.Game;

public class JoinAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task JoinAsync_Creator_RegistersTheirSeatedTeamForThatRoom()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        var (joinedGame, team) = await orchestrator.JoinAsync("conn-1", room.Id, WhitePlayerId);

        Assert.Equal(room.Id, joinedGame.Id);
        Assert.Equal(Team.White, team);
    }

    [Fact]
    public async Task JoinAsync_SecondPlayer_ClaimsRemainingSeatAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        var (joinedGame, team) = await orchestrator.JoinAsync("conn-1", room.Id, BlackPlayerId);

        Assert.Equal(Team.Black, team);
        Assert.Equal(BlackPlayerId, joinedGame.BlackPlayerId);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(BlackPlayerId, gameEntity.BlackPlayerId);
    }

    [Fact]
    public async Task JoinAsync_SecondPlayer_ClaimsWhiteWhenCreatorChoseBlack()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(BlackPlayerId, Team.Black);

        var (joinedGame, team) = await orchestrator.JoinAsync("conn-1", room.Id, WhitePlayerId);

        Assert.Equal(Team.White, team);
        Assert.Equal(WhitePlayerId, joinedGame.WhitePlayerId);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(WhitePlayerId, gameEntity.WhitePlayerId);
    }

    [Fact]
    public async Task JoinAsync_SeatedPlayerReconnecting_RegistersSameTeamWithoutClaimingAgain()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("black-conn-1", room.Id, BlackPlayerId);

        var (_, team) = await orchestrator.JoinAsync("black-conn-2", room.Id, BlackPlayerId);

        Assert.Equal(Team.Black, team);
    }

    [Fact]
    public async Task JoinAsync_UnknownGame_ThrowsGameNotFoundException()
    {
        var (orchestrator, _) = CreateOrchestrator();

        await Assert.ThrowsAsync<GameNotFoundException>(() => orchestrator.JoinAsync("conn-1", "missing-game", WhitePlayerId));
    }

    [Fact]
    public async Task JoinAsync_ThirdPlayer_ThrowsGameFullException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        await Assert.ThrowsAsync<GameFullException>(() => orchestrator.JoinAsync("conn-3", room.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task JoinAsync_BothPlayersJoin_StartsClockFromFullTime()
    {
        var (orchestrator, _, time) = CreateOrchestratorWithClock();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White, TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero));
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        time.Advance(TimeSpan.FromMinutes(3));
        var (joinedGame, _) = await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        Assert.Equal(TimeSpan.FromMinutes(5), joinedGame.State.WhiteTimeRemaining);
    }
}
