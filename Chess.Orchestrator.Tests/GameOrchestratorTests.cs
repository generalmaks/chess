using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Orchestrator.Tests.Support;
using Moq;

namespace Chess.Orchestrator.Tests;

public class GameOrchestratorTests
{
    private static readonly Guid WhitePlayerId = Guid.NewGuid();
    private static readonly Guid BlackPlayerId = Guid.NewGuid();

    private static (GameOrchestrator Orchestrator, MockGameRepository Repository) CreateOrchestrator()
    {
        var repository = new MockGameRepository();
        var orchestrator = new GameOrchestrator(new GameStore(), repository.Object);
        return (orchestrator, repository);
    }

    [Fact]
    public async Task CreateGameAsync_PreferredWhite_SeatsCreatorAsWhiteAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();

        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        Assert.Equal(WhitePlayerId, room.WhitePlayerId);
        Assert.Null(room.BlackPlayerId);

        var added = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.Ongoing, added.Result);
        Assert.Null(added.EndedAtUtc);
        Assert.Equal(WhitePlayerId, added.WhitePlayerId);
        Assert.Null(added.BlackPlayerId);
    }

    [Fact]
    public async Task CreateGameAsync_PreferredBlack_SeatsCreatorAsBlackAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();

        var room = await orchestrator.CreateGameAsync(BlackPlayerId, Team.Black);

        Assert.Equal(BlackPlayerId, room.BlackPlayerId);
        Assert.Null(room.WhitePlayerId);

        var added = Assert.Single(repo.AddedGames);
        Assert.Equal(BlackPlayerId, added.BlackPlayerId);
        Assert.Null(added.WhitePlayerId);
    }

    [Fact]
    public async Task CreateGameAsync_NoPreference_SeatsCreatorInExactlyOneRandomlyChosenTeam()
    {
        var (orchestrator, _) = CreateOrchestrator();

        var sawWhite = false;
        var sawBlack = false;

        // Random assignment is a coin flip; run enough trials that both outcomes are
        // overwhelmingly likely to show up so this isn't a flaky test.
        for (var i = 0; i < 50; i++)
        {
            var room = await orchestrator.CreateGameAsync(Guid.NewGuid());

            // Exactly one seat should be filled - never both, never neither.
            Assert.NotEqual(room.WhitePlayerId is null, room.BlackPlayerId is null);
            sawWhite |= room.WhitePlayerId is not null;
            sawBlack |= room.BlackPlayerId is not null;
        }

        Assert.True(sawWhite);
        Assert.True(sawBlack);
    }

    [Fact]
    public async Task JoinAsync_Creator_RegistersTheirSeatedTeamForThatRoom()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        var (joinedRoom, team) = await orchestrator.JoinAsync("conn-1", room.Id, WhitePlayerId);

        Assert.Same(room, joinedRoom);
        Assert.Equal(Team.White, team);
    }

    [Fact]
    public async Task JoinAsync_SecondPlayer_ClaimsRemainingSeatAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        var (_, team) = await orchestrator.JoinAsync("conn-1", room.Id, BlackPlayerId);

        Assert.Equal(Team.Black, team);
        Assert.Equal(BlackPlayerId, room.BlackPlayerId);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(BlackPlayerId, gameEntity.BlackPlayerId);
    }

    [Fact]
    public async Task JoinAsync_SecondPlayer_ClaimsWhiteWhenCreatorChoseBlack()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(BlackPlayerId, Team.Black);

        var (_, team) = await orchestrator.JoinAsync("conn-1", room.Id, WhitePlayerId);

        Assert.Equal(Team.White, team);
        Assert.Equal(WhitePlayerId, room.WhitePlayerId);

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
    public async Task MakeMoveAsync_WithoutJoiningFirst_ThrowsNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3));

        await Assert.ThrowsAsync<NoActiveConnectionException>(() => orchestrator.MakeMoveAsync("conn-1", move, null));
    }

    [Fact]
    public async Task MakeMoveAsync_OutOfTurn_ThrowsNotYourTurnException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        // White moves first; black tries to move on White's turn.
        var move = new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4));

        await Assert.ThrowsAsync<NotYourTurnException>(() => orchestrator.MakeMoveAsync("black-conn", move, null));
    }

    [Fact]
    public async Task MakeMoveAsync_IllegalMove_ThrowsIllegalMoveException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        // Pawns cannot move four squares in one go.
        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 5));

        await Assert.ThrowsAnyAsync<IllegalMoveException>(() => orchestrator.MakeMoveAsync("white-conn", move, null));
    }

    [Fact]
    public async Task MakeMoveAsync_LegalMove_SwitchesTurnAndPersistsMove()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)); // e2-e4
        var updatedRoom = await orchestrator.MakeMoveAsync("white-conn", move, null);

        Assert.Equal(Team.Black, updatedRoom.State.CurrentTurn);

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
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        orchestrator.OfferDraw("white-conn");
        Assert.Equal(Team.White, room.DrawOfferedBy);

        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), null);

        Assert.Null(room.DrawOfferedBy);
    }

    [Fact]
    public async Task MakeMoveAsync_Checkmate_EndsGameAndPersistsResultAndAllPlies()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        // Fool's mate: 1. f3 e5 2. g4 Qh4#
        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(5, 1), new PieceCord(5, 2)), null);
        await orchestrator.MakeMoveAsync("black-conn", new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)), null);
        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(6, 1), new PieceCord(6, 3)), null);
        var finalRoom = await orchestrator.MakeMoveAsync("black-conn", new PieceMove(new PieceCord(3, 7), new PieceCord(7, 3)), null);

        Assert.Equal(GameResult.BlackWon, finalRoom.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWon, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);

        Assert.Equal(4, repo.AddedMoves.Count);
    }

    [Fact]
    public async Task MakeMoveAsync_AfterGameEnded_ThrowsGameAlreadyEndedException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);
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
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        var updatedRoom = await orchestrator.ResignAsync("white-conn");

        Assert.Equal(GameResult.BlackWon, updatedRoom.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWon, gameEntity.Result);
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
    public void OfferDraw_WithoutJoiningFirst_ThrowsNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();

        Assert.Throws<NoActiveConnectionException>(() => orchestrator.OfferDraw("conn-1"));
    }

    [Fact]
    public async Task OfferDraw_RecordsOfferingTeamOnRoom()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        var (offeringRoom, team) = orchestrator.OfferDraw("black-conn");

        Assert.Equal(Team.Black, team);
        Assert.Equal(Team.Black, offeringRoom.DrawOfferedBy);
    }

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
        Assert.Null(result.Room.DrawOfferedBy);
        Assert.Equal(GameResult.Ongoing, result.Room.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.Ongoing, gameEntity.Result);
        Assert.Null(gameEntity.EndedAtUtc);
        repo.Mock.Verify(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
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
        Assert.Null(result.Room.DrawOfferedBy);
        Assert.Equal(GameResult.DrawByAgreement, result.Room.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.DrawByAgreement, gameEntity.Result);
        Assert.NotNull(gameEntity.EndedAtUtc);
    }

    [Fact]
    public async Task Disconnect_RemovesConnection_SubsequentActionsThrowNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        orchestrator.Disconnect("white-conn");

        Assert.Throws<NoActiveConnectionException>(() => orchestrator.OfferDraw("white-conn"));
    }
}
