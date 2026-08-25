using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;
using Moq;

namespace Chess.Orchestrator.Tests.Game;

public class RespondToDrawAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task RespondToDrawAsync_NoOfferPending_ThrowsNoDrawOfferException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        await Assert.ThrowsAsync<NoDrawOfferException>(() => orchestrator.RespondToDrawAsync("white-conn", true));
    }

    [Fact]
    public async Task RespondToDrawAsync_RespondingToOwnOffer_ThrowsNoDrawOfferException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        orchestrator.OfferDraw("white-conn");

        await Assert.ThrowsAsync<NoDrawOfferException>(() => orchestrator.RespondToDrawAsync("white-conn", true));
    }

    [Fact]
    public async Task RespondToDrawAsync_Decline_ClearsOfferAndLeavesGameOngoingWithoutPersisting()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);
        orchestrator.OfferDraw("white-conn");

        var result = await orchestrator.RespondToDrawAsync("black-conn", accept: false);

        Assert.False(result.Accepted);
        Assert.Null(result.Game.DrawOfferedBy);
        Assert.Equal(GameResult.Ongoing, result.Game.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.Ongoing, gameEntity.Result);
        Assert.Null(gameEntity.EndedAtUtc);
        repo.Mock.Verify(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RespondToDrawAsync_Accept_EndsGameAsDrawAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);
        orchestrator.OfferDraw("white-conn");

        var result = await orchestrator.RespondToDrawAsync("black-conn", accept: true);

        Assert.True(result.Accepted);
        Assert.Null(result.Game.DrawOfferedBy);
        Assert.Equal(GameResult.DrawByAgreement, result.Game.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.DrawByAgreement, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);
    }

    [Fact]
    public async Task RespondToDrawAsync_Accept_EqualRatings_RatingsUnchanged()
    {
        var (orchestrator, _, players) = CreateOrchestratorWithPlayers(whiteRating: 1200, blackRating: 1200);
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);
        orchestrator.OfferDraw("white-conn");

        await orchestrator.RespondToDrawAsync("black-conn", accept: true);

        var white = players.AddedPlayers.Single(p => p.Id == WhitePlayerId);
        var black = players.AddedPlayers.Single(p => p.Id == BlackPlayerId);
        Assert.Equal(1200, white.EloRating);
        Assert.Equal(1200, black.EloRating);
    }
}
