namespace Chess.Orchestrator;

public abstract class GameOrchestrationException(string message) : Exception(message);

public class GameNotFoundException() : GameOrchestrationException("Game not found.");

public class GameFullException() : GameOrchestrationException("This game already has two players.");

public class NoActiveConnectionException() : GameOrchestrationException("Call JoinGame first.");

public class NotYourTurnException() : GameOrchestrationException("It's not your turn.");

public class NoDrawOfferException() : GameOrchestrationException("No draw offer to respond to.");
