namespace Chess.Model.Chess.Game;

public sealed record GameReview
{
    public required Guid Id { get; init; }
    public required GameResult Result { get; init; }

    public Guid? WhitePlayerId { get; init; }
    public string? WhitePlayerName { get; init; }
    public Guid? BlackPlayerId { get; init; }
    public string? BlackPlayerName { get; init; }

    public required IReadOnlyList<GameReviewMove> Moves { get; init; }
}

public sealed record GameReviewMove
{
    public required int FromX { get; init; }
    public required int FromY { get; init; }
    public required int ToX { get; init; }
    public required int ToY { get; init; }
    public char? PromotedTo { get; init; }
}
