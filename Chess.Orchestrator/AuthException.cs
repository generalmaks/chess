namespace Chess.Orchestrator;

// Base for auth failures that are player-facing (bad credentials, taken username, ...)
// rather than bugs, so callers can translate them into a transport-appropriate error.
public abstract class AuthException(string message) : Exception(message);

public class UsernameTakenException() : AuthException("Username is already taken.");

public class InvalidUsernameException() : AuthException("Username must be 3-32 characters.");

public class WeakPasswordException() : AuthException("Password must be at least 8 characters.");

public class InvalidCredentialsException() : AuthException("Invalid username or password.");
