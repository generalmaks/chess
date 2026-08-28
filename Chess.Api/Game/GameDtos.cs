namespace Chess.Api.Game;

public record CreateGameResponse(string GameId, string Team);

public record JoinGameResponse(string Team, GameStateDto State, bool OpponentJoined, MoveHistoryEntryDto[] Moves);

public record MoveRequest(int FromX, int FromY, int ToX, int ToY, char? Promotion);

public record MoveHistoryEntryDto(int FromX, int FromY, int ToX, int ToY, char? PromotedTo);

public record SquareDto(int X, int Y);

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record GameHistoryEntryDto(
    string GameId,
    DateTime CreatedAtUtc,
    DateTime? EndedAtUtc,
    string Result,
    string PlayerColor,
    string Outcome,
    string? OpponentName,
    int? PlayerEloBefore,
    int? PlayerEloAfter,
    int MoveCount);

public record GameReviewDto(
    string GameId,
    string Result,
    string? PlayerColor,
    string? WhitePlayerName,
    string? BlackPlayerName,
    MoveHistoryEntryDto[] Moves);

public record GameStateDto(
    string?[][] Board,
    string CurrentTurn,
    string Result,
    double? WhiteTimeRemainingSeconds,
    double? BlackTimeRemainingSeconds);
