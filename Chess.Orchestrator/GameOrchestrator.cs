using Chess.Dal;
using Chess.Dal.Entities;
using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public readonly record struct DrawResponseResult(GameRoom Room, bool Accepted);

public class GameOrchestrator(GameStore store, IGameRepository repository, IPlayerRepository playerRepository) : IGameOrchestrator
{
    public async Task<GameRoom> CreateGameAsync(Guid playerId, Team? preferredTeam = null, CancellationToken ct = default)
    {
        var team = preferredTeam ?? (Random.Shared.Next(2) == 0 ? Team.White : Team.Black);
        var room = store.CreateGame(playerId, team);

        await repository.AddGameAsync(new GameEntity
        {
            Id = Guid.Parse(room.Id),
            CreatedAtUtc = DateTime.UtcNow,
            WhitePlayerId = room.WhitePlayerId,
            BlackPlayerId = room.BlackPlayerId,
        }, ct);

        return room;
    }

    public async Task<(GameRoom Room, Team Team)> JoinAsync(string connectionId, string gameId, Guid playerId, CancellationToken ct = default)
    {
        var room = store.GetGame(gameId) ?? throw new GameNotFoundException();

        var team = room.TeamForPlayer(playerId);
        if (team is null)
        {
            team = room.TryClaimOpenSeat(playerId) ?? throw new GameFullException();
            await repository.AssignPlayerAsync(Guid.Parse(gameId), team.Value, playerId, ct);
        }

        store.RegisterConnection(connectionId, gameId, team.Value);
        return (room, team.Value);
    }

    public async Task<GameRoom> MakeMoveAsync(string connectionId, PieceMove move, char? promotion, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);
        if (!room.BothPlayersJoined)
            throw new OpponentNotJoinedException();
        if (room.Session.CurrentTurn != team)
            throw new NotYourTurnException();

        room.Session.MakeMove(move, promotion);
        room.DrawOfferedBy = null;

        await PersistMoveAsync(room, team, ct);
        return room;
    }

    public async Task<GameRoom> ResignAsync(string connectionId, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);

        room.Session.Resign(team);
        await EndGameAsync(room, ct);

        return room;
    }

    public (GameRoom Room, Team Team) OfferDraw(string connectionId)
    {
        var (room, team) = GetActiveConnection(connectionId);
        room.DrawOfferedBy = team;
        return (room, team);
    }

    public async Task<DrawResponseResult> RespondToDrawAsync(string connectionId, bool accept, CancellationToken ct = default)
    {
        var (room, team) = GetActiveConnection(connectionId);
        if (room.DrawOfferedBy is null || room.DrawOfferedBy == team)
            throw new NoDrawOfferException();

        room.DrawOfferedBy = null;

        if (!accept)
            return new DrawResponseResult(room, Accepted: false);

        room.Session.AgreeToDraw();
        await EndGameAsync(room, ct);

        return new DrawResponseResult(room, Accepted: true);
    }

    public async Task<GameRoom?> HandleDisconnectAsync(string connectionId, CancellationToken ct = default)
    {
        var connection = store.GetConnection(connectionId);
        store.RemoveConnection(connectionId);

        if (connection is null)
            return null;

        var room = store.GetGame(connection.GameId);
        if (room is null || room.Session.Result != GameResult.Ongoing || !room.BothPlayersJoined)
            return null;

        room.Session.Abandon(connection.Team);
        await EndGameAsync(room, ct);

        return room;
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

        await repository.AddMoveAsync(new MoveEntity
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

    private async Task EndGameAsync(GameRoom room, CancellationToken ct)
    {
        var result = room.Session.Result;
        await repository.EndGameAsync(Guid.Parse(room.Id), result, DateTime.UtcNow, ct);

        if (room.WhitePlayerId is { } whitePlayerId && room.BlackPlayerId is { } blackPlayerId)
            await UpdateRatingsAsync(whitePlayerId, blackPlayerId, result, ct);
    }

    private async Task UpdateRatingsAsync(Guid whitePlayerId, Guid blackPlayerId, GameResult result, CancellationToken ct)
    {
        var whitePlayer = await playerRepository.GetByIdAsync(whitePlayerId, ct);
        var blackPlayer = await playerRepository.GetByIdAsync(blackPlayerId, ct);
        if (whitePlayer is null || blackPlayer is null)
            return;

        var (newWhiteRating, newBlackRating) = EloCalculator.CalculateNewRatings(whitePlayer.EloRating, blackPlayer.EloRating, result);

        await playerRepository.UpdateRatingAsync(whitePlayerId, newWhiteRating, ct);
        await playerRepository.UpdateRatingAsync(blackPlayerId, newBlackRating, ct);
    }
}
