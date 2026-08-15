namespace Chess.Api.Contracts;

public record CreateGameResponse(string GameId, string Team);

public record JoinGameResponse(string Team, GameStateDto State, bool OpponentJoined);

public record MoveRequest(int FromX, int FromY, int ToX, int ToY, char? Promotion);

public record GameStateDto(string?[][] Board, string CurrentTurn, string Result);
