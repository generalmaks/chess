using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Orchestrator.Tests.Support;
using Moq;

namespace Chess.Orchestrator.Tests;

public class GameOrchestratorTests
{
    private static (GameOrchestrator Orchestrator, MockGameRepository Repository) CreateOrchestrator()
    {
        var repository = new MockGameRepository();
        var orchestrator = new GameOrchestrator(new GameStore(), repository.Object);
        return (orchestrator, repository);
    }

    [Fact]
    public async Task CreateGameAsync_PersistsOngoingGameWithNoPlayersAssigned()
    {
        var (orchestrator, repo) = CreateOrchestrator();

        var room = await orchestrator.CreateGameAsync();

        var added = Assert.Single(repo.AddedGames);
        Assert.Equal(Guid.Parse(room.Id), added.Id);
        Assert.Equal(GameResult.Ongoing, added.Result);
        Assert.Null(added.EndedAtUtc);
        Assert.Null(added.WhitePlayerId);
        Assert.Null(added.BlackPlayerId);
    }

    [Fact]
    public async Task Join_ValidWhiteToken_RegistersWhiteTeamForThatRoom()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();

        var (joinedRoom, team) = orchestrator.Join("conn-1", room.Id, room.WhiteToken);

        Assert.Same(room, joinedRoom);
        Assert.Equal(Team.White, team);
    }

    [Fact]
    public async Task Join_ValidBlackToken_RegistersBlackTeam()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();

        var (_, team) = orchestrator.Join("conn-1", room.Id, room.BlackToken);

        Assert.Equal(Team.Black, team);
    }

    [Fact]
    public void Join_UnknownGame_ThrowsGameNotFoundException()
    {
        var (orchestrator, _) = CreateOrchestrator();

        Assert.Throws<GameNotFoundException>(() => orchestrator.Join("conn-1", "missing-game", "any-token"));
    }

    [Fact]
    public async Task Join_InvalidToken_ThrowsInvalidTokenException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();

        Assert.Throws<InvalidTokenException>(() => orchestrator.Join("conn-1", room.Id, "not-a-real-token"));
    }

    [Fact]
    public async Task MakeMoveAsync_WithoutJoiningFirst_ThrowsNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        await orchestrator.CreateGameAsync();

        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3));

        await Assert.ThrowsAsync<NoActiveConnectionException>(() => orchestrator.MakeMoveAsync("conn-1", move, null));
    }

    [Fact]
    public async Task MakeMoveAsync_OutOfTurn_ThrowsNotYourTurnException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("black-conn", room.Id, room.BlackToken);

        // White moves first; black tries to move on White's turn.
        var move = new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4));

        await Assert.ThrowsAsync<NotYourTurnException>(() => orchestrator.MakeMoveAsync("black-conn", move, null));
    }

    [Fact]
    public async Task MakeMoveAsync_IllegalMove_ThrowsIllegalMoveException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);

        // Pawns cannot move four squares in one go.
        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 5));

        await Assert.ThrowsAnyAsync<IllegalMoveException>(() => orchestrator.MakeMoveAsync("white-conn", move, null));
    }

    [Fact]
    public async Task MakeMoveAsync_LegalMove_SwitchesTurnAndPersistsMove()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);

        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)); // e2-e4
        var updatedRoom = await orchestrator.MakeMoveAsync("white-conn", move, null);

        Assert.Equal(Team.Black, updatedRoom.Session.CurrentTurn);

        var moveEntity = Assert.Single(repo.AddedMoves);
        Assert.Equal(0, moveEntity.Ply);
        Assert.Equal(Team.White, moveEntity.Team);
        Assert.Equal('P', moveEntity.PieceCode);
        Assert.Equal(4, moveEntity.FromX);
        Assert.Equal(1, moveEntity.FromY);
        Assert.Equal(4, moveEntity.ToX);
        Assert.Equal(3, moveEntity.ToY);
        Assert.Null(moveEntity.PromotionPieceCode);
        Assert.False(moveEntity.IsCastling);
        Assert.False(moveEntity.IsEnPassant);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.Ongoing, gameEntity.Result);
        Assert.Null(gameEntity.EndedAtUtc);
        repo.Mock.Verify(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MakeMoveAsync_ClearsAnyPendingDrawOffer()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);
        orchestrator.Join("black-conn", room.Id, room.BlackToken);

        orchestrator.OfferDraw("white-conn");
        Assert.Equal(Team.White, room.DrawOfferedBy);

        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), null);

        Assert.Null(room.DrawOfferedBy);
    }

    [Fact]
    public async Task MakeMoveAsync_Checkmate_EndsGameAndPersistsResultAndAllPlies()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);
        orchestrator.Join("black-conn", room.Id, room.BlackToken);

        // Fool's mate: 1. f3 e5 2. g4 Qh4#
        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(5, 1), new PieceCord(5, 2)), null);
        await orchestrator.MakeMoveAsync("black-conn", new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)), null);
        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(6, 1), new PieceCord(6, 3)), null);
        var finalRoom = await orchestrator.MakeMoveAsync("black-conn", new PieceMove(new PieceCord(3, 7), new PieceCord(7, 3)), null);

        Assert.Equal(GameResult.BlackWon, finalRoom.Session.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWon, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);

        Assert.Equal(4, repo.AddedMoves.Count);
    }

    [Fact]
    public async Task MakeMoveAsync_AfterGameEnded_ThrowsGameAlreadyEndedException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);
        orchestrator.Join("black-conn", room.Id, room.BlackToken);
        await orchestrator.ResignAsync("white-conn");

        // Resigning doesn't change whose turn it is, so it's still White's turn;
        // the ended-game check must fire before the turn/legality checks would.
        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3));

        await Assert.ThrowsAsync<GameAlreadyEndedException>(() => orchestrator.MakeMoveAsync("white-conn", move, null));
    }

    [Fact]
    public async Task ResignAsync_EndsGameInOpponentsFavorAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);

        var updatedRoom = await orchestrator.ResignAsync("white-conn");

        Assert.Equal(GameResult.BlackWon, updatedRoom.Session.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWon, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);
    }

    [Fact]
    public async Task ResignAsync_WithoutJoiningFirst_ThrowsNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        await orchestrator.CreateGameAsync();

        await Assert.ThrowsAsync<NoActiveConnectionException>(() => orchestrator.ResignAsync("conn-1"));
    }

    [Fact]
    public void OfferDraw_WithoutJoiningFirst_ThrowsNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();

        Assert.Throws<NoActiveConnectionException>(() => orchestrator.OfferDraw("conn-1"));
    }

    [Fact]
    public async Task OfferDraw_RecordsOfferingTeamOnRoom()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("black-conn", room.Id, room.BlackToken);

        var (offeringRoom, team) = orchestrator.OfferDraw("black-conn");

        Assert.Equal(Team.Black, team);
        Assert.Equal(Team.Black, offeringRoom.DrawOfferedBy);
    }

    [Fact]
    public async Task RespondToDrawAsync_NoOfferPending_ThrowsNoDrawOfferException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);

        await Assert.ThrowsAsync<NoDrawOfferException>(() => orchestrator.RespondToDrawAsync("white-conn", true));
    }

    [Fact]
    public async Task RespondToDrawAsync_RespondingToOwnOffer_ThrowsNoDrawOfferException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);
        orchestrator.OfferDraw("white-conn");

        await Assert.ThrowsAsync<NoDrawOfferException>(() => orchestrator.RespondToDrawAsync("white-conn", true));
    }

    [Fact]
    public async Task RespondToDrawAsync_Decline_ClearsOfferAndLeavesGameOngoingWithoutPersisting()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);
        orchestrator.Join("black-conn", room.Id, room.BlackToken);
        orchestrator.OfferDraw("white-conn");

        var result = await orchestrator.RespondToDrawAsync("black-conn", accept: false);

        Assert.False(result.Accepted);
        Assert.Null(result.Room.DrawOfferedBy);
        Assert.Equal(GameResult.Ongoing, result.Room.Session.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.Ongoing, gameEntity.Result);
        Assert.Null(gameEntity.EndedAtUtc);
        repo.Mock.Verify(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RespondToDrawAsync_Accept_EndsGameAsDrawAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);
        orchestrator.Join("black-conn", room.Id, room.BlackToken);
        orchestrator.OfferDraw("white-conn");

        var result = await orchestrator.RespondToDrawAsync("black-conn", accept: true);

        Assert.True(result.Accepted);
        Assert.Null(result.Room.DrawOfferedBy);
        Assert.Equal(GameResult.DrawByAgreement, result.Room.Session.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.DrawByAgreement, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);
    }

    [Fact]
    public async Task Disconnect_RemovesConnection_SubsequentActionsThrowNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync();
        orchestrator.Join("white-conn", room.Id, room.WhiteToken);

        orchestrator.Disconnect("white-conn");

        Assert.Throws<NoActiveConnectionException>(() => orchestrator.OfferDraw("white-conn"));
    }
}
