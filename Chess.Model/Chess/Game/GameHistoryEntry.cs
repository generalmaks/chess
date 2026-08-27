namespace Chess.Model.Chess.Game;

public sealed record GameHistoryEntry
{
    public required Guid Id { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? EndedAtUtc { get; init; }
    public required GameResult Result { get; init; }

    public Guid? WhitePlayerId { get; init; }
    public string? WhitePlayerName { get; init; }
    public Guid? BlackPlayerId { get; init; }
    public string? BlackPlayerName { get; init; }

    public int? WhiteEloBefore { get; init; }
    public int? BlackEloBefore { get; init; }
    public int? WhiteEloAfter { get; init; }
    public int? BlackEloAfter { get; init; }

    public required int MoveCount { get; init; }
}
