namespace Chess.Orchestrator;

public abstract class AuthException(string message) : Exception(message);

public class UsernameTakenException() : AuthException("Username is already taken.");

public class InvalidUsernameException() : AuthException("Username must be 3-32 characters.");

public class WeakPasswordException() : AuthException("Password must be at least 8 characters.");

public class InvalidCredentialsException() : AuthException("Invalid username or password.");
