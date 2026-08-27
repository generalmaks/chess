using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Player;

namespace Chess.Orchestrator.Game;

public class GameOrchestrator(
    GameStore store, 
    IGameRepository repository, 
    IPlayerRepository playerRepository, 
    TimeProvider? timeProvider = null) : IGameOrchestrator
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Model.Chess.Game.Game> CreateGameAsync(Guid playerId, Team? preferredTeam = null, TimeControl? timeControl = null, CancellationToken ct = default)
    {
        var team = preferredTeam ?? (Random.Shared.Next(2) == 0 ? Team.White : Team.Black);
        var room = store.CreateGame(playerId, team, timeControl, _timeProvider);

        await repository.AddGameAsync(new NewGame
        {
            Id = Guid.Parse(room.Id),
            CreatedAtUtc = DateTime.UtcNow,
            WhitePlayerId = room.WhitePlayerId,
            BlackPlayerId = room.BlackPlayerId,
        }, ct);

        return room.ToGame();
    }

    public const int MaxPageSize = 50;
    public const int DefaultPageSize = 20;

    public Task<PagedResult<GameHistoryEntry>> GetPlayerGamesAsync(Guid playerId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return repository.GetPlayerGamesAsync(playerId, page, pageSize, ct);
    }

    public async Task<(Model.Chess.Game.Game Game, Team Team)> JoinAsync(string connectionId, string gameId, Guid playerId, CancellationToken ct = default)
    {
        var room = store.GetGame(gameId) ?? throw new GameNotFoundException();

        var team = room.TeamForPlayer(playerId);
        if (team is null)
        {
            team = room.TryClaimOpenSeat(playerId) ?? throw new GameFullException();
            if (room.BothPlayersJoined)
                room.Session.StartClock();

            await repository.AssignPlayerAsync(Guid.Parse(gameId), team.Value, playerId, ct);

            if (room.BothPlayersJoined)
                await RecordStartingRatingsAsync(Guid.Parse(gameId), room.WhitePlayerId!.Value, room.BlackPlayerId!.Value, ct);
        }

        store.RegisterConnection(connectionId, gameId, team.Value);
        return (room.ToGame(), team.Value);
    }

    public async Task<Model.Chess.Game.Game> MakeMoveAsync(string connectionId, PieceMove move, char? promotion, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);
        if (!room.BothPlayersJoined)
            throw new OpponentNotJoinedException();

        await room.MutationLock.WaitAsync(ct);
        try
        {
            await EndIfTimedOutAsync(room, ct);
            if (room.Session.Result != GameResult.Ongoing)
                throw new GameAlreadyEndedException();
            if (room.Session.CurrentTurn != team)
                throw new NotYourTurnException();

            room.Session.MakeMove(move, promotion);
            room.DrawOfferedBy = null;

            await PersistMoveAsync(room, team, ct);
            return room.ToGame();
        }
        finally
        {
            room.MutationLock.Release();
        }
    }

    public async Task<Model.Chess.Game.Game> ResignAsync(string connectionId, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);

        await room.MutationLock.WaitAsync(ct);
        try
        {
            await EndIfTimedOutAsync(room, ct);
            room.Session.Resign(team);
            await EndGameAsync(room, ct);
            return room.ToGame();
        }
        finally
        {
            room.MutationLock.Release();
        }
    }

    public (Model.Chess.Game.Game Game, Team Team) OfferDraw(string connectionId)
    {
        var (room, team) = GetActiveConnection(connectionId);
        room.DrawOfferedBy = team;
        return (room.ToGame(), team);
    }

    public PieceMove[] GetLegalMoves(string connectionId, PieceCord from)
    {
        var (room, _) = GetActiveConnection(connectionId);
        return room.Session.Board.GetLegalMoves(from);
    }

    public async Task<DrawResponseResult> RespondToDrawAsync(string connectionId, bool accept, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);

        await room.MutationLock.WaitAsync(ct);
        try
        {
            await EndIfTimedOutAsync(room, ct);
            if (room.DrawOfferedBy is null || room.DrawOfferedBy == team)
                throw new NoDrawOfferException();

            room.DrawOfferedBy = null;

            if (!accept)
                return new DrawResponseResult(room.ToGame(), Accepted: false);

            room.Session.AgreeToDraw();
            await EndGameAsync(room, ct);

            return new DrawResponseResult(room.ToGame(), Accepted: true);
        }
        finally
        {
            room.MutationLock.Release();
        }
    }

    public async Task<Model.Chess.Game.Game?> HandleDisconnectAsync(string connectionId, CancellationToken ct = default)
    {
        var connection = store.GetConnection(connectionId);
        store.RemoveConnection(connectionId);

        if (connection is null)
            return null;

        var room = store.GetGame(connection.GameId);
        if (room is null || room.Session.Result != GameResult.Ongoing || !room.BothPlayersJoined)
            return null;

        await room.MutationLock.WaitAsync(ct);
        try
        {
            if (await EndIfTimedOutAsync(room, ct))
                return room.ToGame();
            if (room.Session.Result != GameResult.Ongoing)
                return null;

            room.Session.Abandon(connection.Team);
            await EndGameAsync(room, ct);
            return room.ToGame();
        }
        finally
        {
            room.MutationLock.Release();
        }
    }

    public async Task<Model.Chess.Game.Game?> CheckTimeoutAsync(string gameId, CancellationToken ct = default)
    {
        var room = store.GetGame(gameId);
        if (room is null)
            return null;

        await room.MutationLock.WaitAsync(ct);
        try
        {
            return await EndIfTimedOutAsync(room, ct) ? room.ToGame() : null;
        }
        finally
        {
            room.MutationLock.Release();
        }
    }

    private async Task<bool> EndIfTimedOutAsync(GameRoom room, CancellationToken ct)
    {
        if (!room.BothPlayersJoined || !room.Session.CheckTimeout())
            return false;

        await EndGameAsync(room, ct);
        return true;
    }

    private (GameRoom Room, Team Team) GetActiveConnection(string connectionId)
    {
        var connection = store.GetConnection(connectionId) ?? throw new NoActiveConnectionException();
        var room = store.GetGame(connection.GameId) ?? throw new GameNotFoundException();
        return (room, connection.Team);
    }

    private async Task PersistMoveAsync(GameRoom room, Team team, CancellationToken ct)
    {
        var lastMove = room.Session.Board.MoveHistory[^1];

        await repository.AddMoveAsync(new RecordedMove
        {
            GameId = Guid.Parse(room.Id),
            Ply = room.Session.Board.MoveHistory.Count - 1,
            Team = team,
            PieceCode = lastMove.PieceCode,
            FromX = lastMove.Move.From.X,
            FromY = lastMove.Move.From.Y,
            ToX = lastMove.Move.To.X,
            ToY = lastMove.Move.To.Y,
            PromotionPieceCode = lastMove.PromotedTo,
            IsCastling = lastMove.IsCastling,
            IsEnPassant = lastMove.IsEnPassant,
            PlayedAtUtc = DateTime.UtcNow,
        }, ct);

        if (room.Session.Result != GameResult.Ongoing)
            await EndGameAsync(room, ct);
    }

    private async Task RecordStartingRatingsAsync(Guid gameId, Guid whitePlayerId, Guid blackPlayerId, CancellationToken ct)
    {
        var whitePlayer = await playerRepository.GetByIdAsync(whitePlayerId, ct);
        var blackPlayer = await playerRepository.GetByIdAsync(blackPlayerId, ct);
        if (whitePlayer is null || blackPlayer is null)
            return;

        await repository.SetStartingRatingsAsync(gameId, whitePlayer.EloRating, blackPlayer.EloRating, ct);
    }

    private async Task EndGameAsync(GameRoom room, CancellationToken ct)
    {
        var result = room.Session.Result;

        int? newWhiteRating = null;
        int? newBlackRating = null;
        if (room.WhitePlayerId is { } whitePlayerId && room.BlackPlayerId is { } blackPlayerId)
            (newWhiteRating, newBlackRating) = await UpdateRatingsAsync(whitePlayerId, blackPlayerId, result, ct);

        await repository.EndGameAsync(Guid.Parse(room.Id), result, DateTime.UtcNow, newWhiteRating, newBlackRating, ct);
    }

    private async Task<(int? WhiteRating, int? BlackRating)> UpdateRatingsAsync(Guid whitePlayerId, Guid blackPlayerId, GameResult result, CancellationToken ct)
    {
        var whitePlayer = await playerRepository.GetByIdAsync(whitePlayerId, ct);
        var blackPlayer = await playerRepository.GetByIdAsync(blackPlayerId, ct);
        if (whitePlayer is null || blackPlayer is null)
            return (null, null);

        var (newWhiteRating, newBlackRating) = EloCalculator.CalculateNewRatings(whitePlayer.EloRating, blackPlayer.EloRating, result);

        await playerRepository.UpdateRatingAsync(whitePlayerId, newWhiteRating, ct);
        await playerRepository.UpdateRatingAsync(blackPlayerId, newBlackRating, ct);

        return (newWhiteRating, newBlackRating);
    }
}
