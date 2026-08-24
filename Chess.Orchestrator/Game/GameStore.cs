using System.Collections.Concurrent;
using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public record PlayerConnection(string GameId, Team Team);

public class GameStore
{
    private readonly ConcurrentDictionary<string, GameRoom> _games = new();
    private readonly ConcurrentDictionary<string, PlayerConnection> _connections = new();

    public GameRoom CreateGame(Guid creatorPlayerId, Team creatorTeam, TimeControl? timeControl = null, TimeProvider? timeProvider = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var room = new GameRoom(id, timeControl, timeProvider);
        room.SeatCreator(creatorPlayerId, creatorTeam);
        _games[id] = room;
        return room;
    }

    public GameRoom? GetGame(string gameId) => _games.GetValueOrDefault(gameId);

    public IEnumerable<GameRoom> ActiveTimedGames =>
        _games.Values.Where(g => g.Session.TimeControl is not null && g.Session.Result == GameResult.Ongoing);

    public void RegisterConnection(string connectionId, string gameId, Team team) =>
        _connections[connectionId] = new PlayerConnection(gameId, team);

    public PlayerConnection? GetConnection(string connectionId) => _connections.GetValueOrDefault(connectionId);

    public void RemoveConnection(string connectionId) => _connections.TryRemove(connectionId, out _);
}
