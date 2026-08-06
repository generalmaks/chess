namespace Chess.Orchestrator;

// Base for orchestration failures that are player-facing (bad game id, wrong turn, ...)
// rather than bugs, so callers can translate them into a transport-appropriate error.
public abstract class GameOrchestrationException(string message) : Exception(message);

public class GameNotFoundException() : GameOrchestrationException("Game not found.");

public class InvalidTokenException() : GameOrchestrationException("Invalid token.");

public class NoActiveConnectionException() : GameOrchestrationException("Call JoinGame first.");

public class NotYourTurnException() : GameOrchestrationException("It's not your turn.");

public class NoDrawOfferException() : GameOrchestrationException("No draw offer to respond to.");
