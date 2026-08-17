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
}
