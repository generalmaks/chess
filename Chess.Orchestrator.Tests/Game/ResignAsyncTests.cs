using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;
using Moq;

namespace Chess.Orchestrator.Tests.Game;

public class ResignAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task ResignAsync_EndsGameInOpponentsFavorAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        var updatedRoom = await orchestrator.ResignAsync("white-conn");

        Assert.Equal(GameResult.BlackWonByResignation, updatedRoom.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWonByResignation, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);
    }

    [Fact]
    public async Task ResignAsync_WithoutJoiningFirst_ThrowsNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        await Assert.ThrowsAsync<NoActiveConnectionException>(() => orchestrator.ResignAsync("conn-1"));
    }

    [Fact]
    public async Task ResignAsync_RecordsEloBeforeAndAfterOnGameEntity()
    {
        var (orchestrator, gameRepository, _) = CreateOrchestratorWithPlayers(whiteRating: 1600, blackRating: 1200);
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        await orchestrator.ResignAsync("white-conn");

        var gameEntity = Assert.Single(gameRepository.AddedGames);
        Assert.Equal(1600, gameEntity.WhiteEloBefore);
        Assert.Equal(1200, gameEntity.BlackEloBefore);
        Assert.NotNull(gameEntity.WhiteEloAfter);
        Assert.NotNull(gameEntity.BlackEloAfter);
        Assert.NotEqual(gameEntity.WhiteEloBefore, gameEntity.WhiteEloAfter);
        Assert.NotEqual(gameEntity.BlackEloBefore, gameEntity.BlackEloAfter);
    }

    [Fact]
    public async Task ResignAsync_EqualRatings_WinnerGainsAndLoserLosesSameAmount()
    {
        var (orchestrator, _, players) = CreateOrchestratorWithPlayers(whiteRating: 1200, blackRating: 1200);
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        await orchestrator.ResignAsync("white-conn");

        var white = players.AddedPlayers.Single(p => p.Id == WhitePlayerId);
        var black = players.AddedPlayers.Single(p => p.Id == BlackPlayerId);
        Assert.Equal(1184, white.EloRating);
        Assert.Equal(1216, black.EloRating);
    }

    [Fact]
    public async Task ResignAsync_HigherRatedPlayerLosingToLowerRated_LosesMoreThanEqualRatedCase()
    {
        var (orchestrator, _, players) = CreateOrchestratorWithPlayers(whiteRating: 1600, blackRating: 1200);
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        await orchestrator.ResignAsync("white-conn");

        var white = players.AddedPlayers.Single(p => p.Id == WhitePlayerId);
        var black = players.AddedPlayers.Single(p => p.Id == BlackPlayerId);
        Assert.True(white.EloRating < 1600 - 16, "Favorite should lose more than 16 points for an upset loss.");
        Assert.True(black.EloRating > 1200 + 16, "Underdog should gain more than 16 points for an upset win.");
    }

    [Fact]
    public async Task ResignAsync_OpponentSeatNeverFilled_DoesNotAttemptRatingUpdate()
    {
        var (orchestrator, _, players) = CreateOrchestratorWithPlayers();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        await orchestrator.ResignAsync("white-conn");

        players.Mock.Verify(r => r.UpdateRatingAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResignAsync_TimeAlreadyExpired_PersistsTimeoutInsteadOfResignationAndThrows()
    {
        var (orchestrator, repo, time) = CreateOrchestratorWithClock();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White, TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero));
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        time.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<GameAlreadyEndedException>(() => orchestrator.ResignAsync("white-conn"));

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWonOnTime, gameEntity.Result);
    }
}
