using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public class GameRoom(string id)
{
    private readonly object _joinLock = new();

    public string Id { get; } = id;

    // Internal: GameOrchestrator is the only thing allowed to mutate a session (turn
    // checks, draw-offer rules, ...). Outside Chess.Orchestrator, use State instead.
    internal ChessGameSession Session { get; } = new();

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

    // Used once, right after construction, to seat the creator in their chosen (or
    // randomly rolled) team. No locking needed: nobody else can reach this room yet.
    internal void SeatCreator(Guid playerId, Team team)
    {
        if (team == Team.White) WhitePlayerId = playerId;
        else BlackPlayerId = playerId;
    }

    // Atomically claims whichever seat is still open for a joining player. Returns null if
    // both seats are already taken.
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
