using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chess.Api.Game;

[Authorize]
public class ChessHub(IGameOrchestrator orchestrator) : Hub
{
    public async Task<JoinGameResponse> JoinGame(string gameId)
    {
        var (game, team) = await GuardedAsync(() => orchestrator.JoinAsync(Context.ConnectionId, gameId, GetPlayerId()));

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(gameId));

        var joinerName = team == Team.White ? game.WhitePlayerName : game.BlackPlayerName;
        await Clients.OthersInGroup(GroupName(gameId)).SendAsync("PlayerJoined", team.ToString(), joinerName);

        return new JoinGameResponse(
            team.ToString(),
            GameStateMapper.ToDto(game.State),
            game.BothPlayersJoined,
            GameStateMapper.ToMoveHistoryDto(game.MoveHistory),
            game.WhitePlayerName,
            game.BlackPlayerName);
    }

    public async Task MakeMove(MoveRequest request)
    {
        var move = new PieceMove(new PieceCord(request.FromX, request.FromY), new PieceCord(request.ToX, request.ToY));
        var game = await GuardedAsync(() => orchestrator.MakeMoveAsync(Context.ConnectionId, move, request.Promotion));

        await Clients.Group(GroupName(game.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(game.State));
    }

    public Task<SquareDto[]> GetLegalMoves(int x, int y)
    {
        var moves = Guarded(() => orchestrator.GetLegalMoves(Context.ConnectionId, new PieceCord(x, y)));
        return Task.FromResult(moves.Select(m => new SquareDto(m.To.X, m.To.Y)).ToArray());
    }

    public async Task Resign()
    {
        var game = await GuardedAsync(() => orchestrator.ResignAsync(Context.ConnectionId));

        await Clients.Group(GroupName(game.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(game.State));
    }

    public async Task OfferDraw()
    {
        var (game, team) = Guarded(() => orchestrator.OfferDraw(Context.ConnectionId));
        await Clients.OthersInGroup(GroupName(game.Id)).SendAsync("DrawOffered", team.ToString());
    }

    public async Task RespondToDraw(bool accept)
    {
        var result = await GuardedAsync(() => orchestrator.RespondToDrawAsync(Context.ConnectionId, accept));

        if (!result.Accepted)
        {
            await Clients.Group(GroupName(result.Game.Id)).SendAsync("DrawDeclined");
            return;
        }

        await Clients.Group(GroupName(result.Game.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(result.Game.State));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var game = await orchestrator.HandleDisconnectAsync(Context.ConnectionId);
        if (game is not null)
        {
            await Clients.Group(GroupName(game.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(game.State));
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetPlayerId() => Guid.Parse(Context.User!.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    internal static string GroupName(string gameId) => $"game-{gameId}";
    
    private static T Guarded<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is IllegalMoveException or GameAlreadyEndedException or GameOrchestrationException)
        {
            throw new HubException(ex.Message);
        }
    }

    private static async Task<T> GuardedAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is IllegalMoveException or GameAlreadyEndedException or GameOrchestrationException)
        {
            throw new HubException(ex.Message);
        }
    }
}
