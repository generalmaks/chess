using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;
using Moq;

namespace Chess.Orchestrator.Tests.Game;

public class HandleDisconnectAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task HandleDisconnectAsync_RemovesConnection_SubsequentActionsThrowNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        await orchestrator.HandleDisconnectAsync("white-conn");

        Assert.Throws<NoActiveConnectionException>(() => orchestrator.OfferDraw("white-conn"));
    }

    [Fact]
    public async Task HandleDisconnectAsync_UnknownConnection_ReturnsNull()
    {
        var (orchestrator, _) = CreateOrchestrator();

        var room = await orchestrator.HandleDisconnectAsync("missing-conn");

        Assert.Null(room);
    }

    [Fact]
    public async Task HandleDisconnectAsync_OpponentNotJoinedYet_DoesNotEndGame()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        var result = await orchestrator.HandleDisconnectAsync("white-conn");

        Assert.Null(result);
        Assert.Equal(GameResult.Ongoing, room.State.Result);
        repo.Mock.Verify(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleDisconnectAsync_MidGame_AbandonsDisconnectingPlayerAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        var result = await orchestrator.HandleDisconnectAsync("white-conn");

        Assert.NotNull(result);
        Assert.Equal(GameResult.BlackWonByAbandonment, result!.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWonByAbandonment, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);
    }

    [Fact]
    public async Task HandleDisconnectAsync_AfterGameAlreadyEnded_DoesNotResignAgain()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);
        await orchestrator.ResignAsync("white-conn");

        var result = await orchestrator.HandleDisconnectAsync("black-conn");

        Assert.Null(result);
        repo.Mock.Verify(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
