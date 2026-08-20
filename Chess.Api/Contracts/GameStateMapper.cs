using Chess.Logic.Board;
using Chess.Orchestrator;

namespace Chess.Api.Contracts;

public static class GameStateMapper
{
    public static GameStateDto ToDto(GameStateSnapshot state) =>
        new(
            state.Board,
            state.CurrentTurn.ToString(),
            state.Result.ToString(),
            state.WhiteTimeRemaining?.TotalSeconds,
            state.BlackTimeRemaining?.TotalSeconds);

    public static MoveHistoryEntryDto[] ToMoveHistoryDto(IReadOnlyList<MoveRecord> history) =>
        history
            .Select(move => new MoveHistoryEntryDto(move.Move.From.X, move.Move.From.Y, move.Move.To.X, move.Move.To.Y, move.PromotedTo))
            .ToArray();
}
