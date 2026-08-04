using Chess.Api.Contracts;
using Chess.Api.Games;
using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Microsoft.AspNetCore.SignalR;

namespace Chess.Api.Hubs;

public class ChessHub(GameStore store) : Hub
{
    public async Task<JoinGameResponse> JoinGame(string gameId, string token)
    {
        var room = store.GetGame(gameId) ?? throw new HubException("Game not found.");
        var team = room.TeamForToken(token) ?? throw new HubException("Invalid token.");

        store.RegisterConnection(Context.ConnectionId, gameId, team);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(gameId));
        await Clients.OthersInGroup(GroupName(gameId)).SendAsync("PlayerJoined", team.ToString());

        return new JoinGameResponse(team.ToString(), GameStateMapper.ToDto(room.Session));
    }

    public async Task MakeMove(MoveRequest request)
    {
        var (room, team) = GetActiveConnection();

        if (room.Session.CurrentTurn != team)
            throw new HubException("It's not your turn.");

        var move = new PieceMove(new PieceCord(request.FromX, request.FromY), new PieceCord(request.ToX, request.ToY));
        RunSessionAction(() => room.Session.MakeMove(move, request.Promotion));

        room.DrawOfferedBy = null;
        await Clients.Group(GroupName(room.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(room.Session));
    }

    public async Task Resign()
    {
        var (room, team) = GetActiveConnection();

        RunSessionAction(() => room.Session.Resign(team));

        await Clients.Group(GroupName(room.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(room.Session));
    }

    public async Task OfferDraw()
    {
        var (room, team) = GetActiveConnection();

        room.DrawOfferedBy = team;
        await Clients.OthersInGroup(GroupName(room.Id)).SendAsync("DrawOffered", team.ToString());
    }

    public async Task RespondToDraw(bool accept)
    {
        var (room, team) = GetActiveConnection();

        if (room.DrawOfferedBy is null || room.DrawOfferedBy == team)
            throw new HubException("No draw offer to respond to.");

        room.DrawOfferedBy = null;

        if (!accept)
        {
            await Clients.Group(GroupName(room.Id)).SendAsync("DrawDeclined");
            return;
        }

        RunSessionAction(room.Session.AgreeToDraw);
        await Clients.Group(GroupName(room.Id)).SendAsync("StateUpdated", GameStateMapper.ToDto(room.Session));
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        store.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private (GameRoom Room, Team Team) GetActiveConnection()
    {
        var connection = store.GetConnection(Context.ConnectionId) ?? throw new HubException("Call JoinGame first.");
        var room = store.GetGame(connection.GameId) ?? throw new HubException("Game no longer exists.");
        return (room, connection.Team);
    }

    // Domain exceptions carry player-facing messages (illegal move, game already
    // over); everything else is a real bug and should surface as a 500/crash, not
    // get swallowed into a HubException.
    private static void RunSessionAction(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is IllegalMoveException or GameAlreadyEndedException)
        {
            throw new HubException(ex.Message);
        }
    }

    private static string GroupName(string gameId) => $"game-{gameId}";
}
