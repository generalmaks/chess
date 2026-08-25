using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;
using Moq;

namespace Chess.Orchestrator.Tests.Game;

public class MakeMoveAsyncTests : GameOrchestratorTestBase
{
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
    public async Task MakeMoveAsync_OpponentNotJoined_ThrowsOpponentNotJoinedException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);

        var move = new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3));

        await Assert.ThrowsAsync<OpponentNotJoinedException>(() => orchestrator.MakeMoveAsync("white-conn", move, null));
    }

    [Fact]
    public async Task MakeMoveAsync_IllegalMove_ThrowsIllegalMoveException()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

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
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

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
        repo.Mock.Verify(r => r.EndGameAsync(It.IsAny<Guid>(), It.IsAny<GameResult>(), It.IsAny<DateTime>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MakeMoveAsync_ClearsAnyPendingDrawOffer()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        var (afterOffer, _) = orchestrator.OfferDraw("white-conn");
        Assert.Equal(Team.White, afterOffer.DrawOfferedBy);

        var afterMove = await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), null);

        Assert.Null(afterMove.DrawOfferedBy);
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

        Assert.Equal(GameResult.BlackWonByCheckmate, finalRoom.State.Result);

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWonByCheckmate, gameEntity.Result);
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
    public async Task MakeMoveAsync_Checkmate_UpdatesBothPlayersRatings()
    {
        var (orchestrator, _, players) = CreateOrchestratorWithPlayers(whiteRating: 1200, blackRating: 1200);
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        // Fool's mate: 1. f3 e5 2. g4 Qh4#
        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(5, 1), new PieceCord(5, 2)), null);
        await orchestrator.MakeMoveAsync("black-conn", new PieceMove(new PieceCord(4, 6), new PieceCord(4, 4)), null);
        await orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(6, 1), new PieceCord(6, 3)), null);
        await orchestrator.MakeMoveAsync("black-conn", new PieceMove(new PieceCord(3, 7), new PieceCord(7, 3)), null);

        var white = players.AddedPlayers.Single(p => p.Id == WhitePlayerId);
        var black = players.AddedPlayers.Single(p => p.Id == BlackPlayerId);
        Assert.Equal(1184, white.EloRating);
        Assert.Equal(1216, black.EloRating);
    }

    [Fact]
    public async Task MakeMoveAsync_TimeExpired_EndsGameOnTimeAndThrows()
    {
        var (orchestrator, repo, time) = CreateOrchestratorWithClock();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White, TimeControl.Create(TimeSpan.FromMinutes(5), TimeSpan.Zero));
        await orchestrator.JoinAsync("white-conn", room.Id, WhitePlayerId);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        time.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<GameAlreadyEndedException>(() =>
            orchestrator.MakeMoveAsync("white-conn", new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), null));

        var gameEntity = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.BlackWonOnTime, gameEntity.Result);
    }
}
