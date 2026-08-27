namespace Chess.Api.Game;

public record CreateGameResponse(string GameId, string Team);

public record JoinGameResponse(string Team, GameStateDto State, bool OpponentJoined, MoveHistoryEntryDto[] Moves);

public record MoveRequest(int FromX, int FromY, int ToX, int ToY, char? Promotion);

public record MoveHistoryEntryDto(int FromX, int FromY, int ToX, int ToY, char? PromotedTo);

public record SquareDto(int X, int Y);

public record GameStateDto(
    string?[][] Board,
    string CurrentTurn,
    string Result,
    double? WhiteTimeRemainingSeconds,
    double? BlackTimeRemainingSeconds);
