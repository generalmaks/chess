using Chess.Model.Chess.Board;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Api.Game;

public static class GameStateMapper
{
    public static PagedResultDto<GameHistoryEntryDto> ToHistoryDto(PagedResult<GameHistoryEntry> page, Guid playerId) =>
        new(
            page.Items.Select(entry => ToHistoryEntryDto(entry, playerId)).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalCount,
            page.TotalPages);

    private static GameHistoryEntryDto ToHistoryEntryDto(GameHistoryEntry entry, Guid playerId)
    {
        var playerIsWhite = entry.WhitePlayerId == playerId;
        var playerColor = playerIsWhite ? Team.White : Team.Black;

        return new GameHistoryEntryDto(
            entry.Id.ToString("N"),
            entry.CreatedAtUtc,
            entry.EndedAtUtc,
            entry.Result.ToString(),
            playerColor.ToString(),
            OutcomeFor(entry.Result, playerColor),
            playerIsWhite ? entry.BlackPlayerName : entry.WhitePlayerName,
            playerIsWhite ? entry.WhiteEloBefore : entry.BlackEloBefore,
            playerIsWhite ? entry.WhiteEloAfter : entry.BlackEloAfter,
            entry.MoveCount);
    }

    private static string OutcomeFor(GameResult result, Team playerColor) => result switch
    {
        GameResult.Ongoing => "Ongoing",
        GameResult.StalemateDraw or GameResult.FiftyMoveRuleDraw or GameResult.DrawByAgreement
            or GameResult.InsufficientMaterialDraw or GameResult.ThreefoldRepetitionDraw
            or GameResult.TimeoutInsufficientMaterialDraw => "Draw",
        GameResult.WhiteWonByCheckmate or GameResult.WhiteWonByResignation
            or GameResult.WhiteWonByAbandonment or GameResult.WhiteWonOnTime =>
            playerColor == Team.White ? "Win" : "Loss",
        GameResult.BlackWonByCheckmate or GameResult.BlackWonByResignation
            or GameResult.BlackWonByAbandonment or GameResult.BlackWonOnTime =>
            playerColor == Team.Black ? "Win" : "Loss",
        _ => "Unknown",
    };

    public static GameReviewDto ToReviewDto(GameReview review, Guid playerId)
    {
        string? playerColor =
            review.WhitePlayerId == playerId ? nameof(Team.White)
            : review.BlackPlayerId == playerId ? nameof(Team.Black)
            : null;

        return new GameReviewDto(
            review.Id.ToString("N"),
            review.Result.ToString(),
            playerColor,
            review.WhitePlayerName,
            review.BlackPlayerName,
            review.Moves
                .Select(m => new MoveHistoryEntryDto(m.FromX, m.FromY, m.ToX, m.ToY, m.PromotedTo, m.San))
                .ToArray());
    }

    public static GameStateDto ToDto(GameStateSnapshot state) =>
        new(
            state.Board,
            state.CurrentTurn.ToString(),
            state.Result.ToString(),
            state.WhiteTimeRemaining?.TotalSeconds,
            state.BlackTimeRemaining?.TotalSeconds,
            state.LastMove is { } move ? ToMoveDto(move) : null);

    public static MoveHistoryEntryDto[] ToMoveHistoryDto(IReadOnlyList<MoveRecord> history) =>
        history.Select(ToMoveDto).ToArray();

    private static MoveHistoryEntryDto ToMoveDto(MoveRecord move) =>
        new(move.Move.From.X, move.Move.From.Y, move.Move.To.X, move.Move.To.Y, move.PromotedTo, move.San);
}
