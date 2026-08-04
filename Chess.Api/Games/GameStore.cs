using System.Collections.Concurrent;
using Chess.Logic.Pieces.Chess;

namespace Chess.Api.Games;

public record PlayerConnection(string GameId, Team Team);

public class GameStore
{
    private readonly ConcurrentDictionary<string, GameRoom> _games = new();
    private readonly ConcurrentDictionary<string, PlayerConnection> _connections = new();

    public GameRoom CreateGame()
    {
        var id = Guid.NewGuid().ToString("N");
        var room = new GameRoom(id, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"));
        _games[id] = room;
        return room;
    }

    public GameRoom? GetGame(string gameId) => _games.GetValueOrDefault(gameId);

    public void RegisterConnection(string connectionId, string gameId, Team team) =>
        _connections[connectionId] = new PlayerConnection(gameId, team);

    public PlayerConnection? GetConnection(string connectionId) => _connections.GetValueOrDefault(connectionId);

    public void RemoveConnection(string connectionId) => _connections.TryRemove(connectionId, out _);
}
