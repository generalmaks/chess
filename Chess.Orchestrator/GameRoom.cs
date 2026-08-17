using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public class GameRoom(string id, TimeControl? timeControl = null, TimeProvider? timeProvider = null)
{
    private readonly object _joinLock = new();

    public string Id { get; } = id;

    internal SemaphoreSlim MutationLock { get; } = new(1, 1);

    // Internal: GameOrchestrator is the only thing allowed to mutate a session (turn
    // checks, draw-offer rules, ...). Outside Chess.Orchestrator, use State instead.
    internal ChessGameSession Session { get; } = new(timeControl, timeProvider);

    public GameStateSnapshot State => GameStateSnapshot.Capture(Session);

    public Guid? WhitePlayerId { get; private set; }
    public Guid? BlackPlayerId { get; private set; }

    public bool BothPlayersJoined => WhitePlayerId is not null && BlackPlayerId is not null;

    // Set while one player has offered a draw and the other hasn't responded yet.
    public Team? DrawOfferedBy { get; set; }

    public Team? TeamForPlayer(Guid playerId)
    {
        if (playerId == WhitePlayerId) return Team.White;
        if (playerId == BlackPlayerId) return Team.Black;
        return null;
    }
    
    internal void SeatCreator(Guid playerId, Team team)
    {
        if (team == Team.White) WhitePlayerId = playerId;
        else BlackPlayerId = playerId;
    }
    
    internal Team? TryClaimOpenSeat(Guid playerId)
    {
        lock (_joinLock)
        {
            if (WhitePlayerId is null) { WhitePlayerId = playerId; return Team.White; }
            if (BlackPlayerId is null) { BlackPlayerId = playerId; return Team.Black; }
            return null;
        }
    }
}
