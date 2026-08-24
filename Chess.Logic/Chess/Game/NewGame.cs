namespace Chess.Logic;

public sealed record NewGame
{
    public required Guid Id { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public Guid? WhitePlayerId { get; init; }
    public Guid? BlackPlayerId { get; init; }
}
