using Chess.Api.Contracts;
using Chess.Logic;
using Chess.Logic.Board;
using Chess.Orchestrator;
using Microsoft.AspNetCore.SignalR;

namespace Chess.Api.Hubs;

public class ChessHub(GameOrchestrator orchestrator) : Hub
{
    public async Task<JoinGameResponse> JoinGame(string gameId, string token)
    {
        var (room, team) = Guarded(() => orchestrator.Join(Context.ConnectionId, gameId, token));

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(gameId));
        await Clients.OthersInGroup(GroupName(gameId)).SendAsync("PlayerJoined", team.ToString());

        return new JoinGameResponse(team.ToString(), GameStateMapper.ToDto(room.Session));
    }

    public async Task MakeMove(MoveRequest request)
    {
        var move = new PieceMove(new PieceCord(request.FromX, request.FromY), new PieceCord(request.ToX, request.ToY));
        var room = await GuardedAsync(() => orchestrator.MakeMoveAsync(Context.ConnectionId, move, request.Promotion));

        await Clients.Group(GroupName(room.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(room.Session));
    }

    public async Task Resign()
    {
        var room = await GuardedAsync(() => orchestrator.ResignAsync(Context.ConnectionId));

        await Clients.Group(GroupName(room.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(room.Session));
    }

    public async Task OfferDraw()
    {
        var (room, team) = Guarded(() => orchestrator.OfferDraw(Context.ConnectionId));
        await Clients.OthersInGroup(GroupName(room.Id)).SendAsync("DrawOffered", team.ToString());
    }

    public async Task RespondToDraw(bool accept)
    {
        var result = await GuardedAsync(() => orchestrator.RespondToDrawAsync(Context.ConnectionId, accept));

        if (!result.Accepted)
        {
            await Clients.Group(GroupName(result.Room.Id)).SendAsync("DrawDeclined");
            return;
        }

        await Clients.Group(GroupName(result.Room.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(result.Room.Session));
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        orchestrator.Disconnect(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private static string GroupName(string gameId) => $"game-{gameId}";

    // Domain/orchestration exceptions carry player-facing messages (illegal move, game
    // already over, unknown game, ...); everything else is a real bug and should surface
    // as a 500/crash, not get swallowed into a HubException.
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
