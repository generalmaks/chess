using Chess.Logic;
using Chess.Logic.Pieces.Chess;

namespace Chess.Orchestrator;

public class GameRoom(string id, string whiteToken, string blackToken)
{
    public string Id { get; } = id;

    // Internal: GameOrchestrator is the only thing allowed to mutate a session (turn
    // checks, draw-offer rules, ...). Outside Chess.Orchestrator, use State instead.
    internal ChessGameSession Session { get; } = new();

    public GameStateSnapshot State => GameStateSnapshot.Capture(Session);

    public string WhiteToken { get; } = whiteToken;
    public string BlackToken { get; } = blackToken;

    // Set while one player has offered a draw and the other hasn't responded yet.
    public Team? DrawOfferedBy { get; set; }

    public Team? TeamForToken(string token)
    {
        if (token == WhiteToken) return Team.White;
        if (token == BlackToken) return Team.Black;
        return null;
    }
}
