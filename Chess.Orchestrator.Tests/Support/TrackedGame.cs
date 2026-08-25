using Chess.Model.Chess.Game;

namespace Chess.Orchestrator.Tests.Support;

// Mutable projection of a persisted game row, tracked purely for test assertions
// against MockGameRepository (NewGame itself is an immutable input DTO).
public class TrackedGame
{
    public required Guid Id { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public Guid? WhitePlayerId { get; set; }
    public Guid? BlackPlayerId { get; set; }
    public GameResult Result { get; set; } = GameResult.Ongoing;
    public DateTime? EndedAtUtc { get; set; }
    public int? WhiteEloBefore { get; set; }
    public int? BlackEloBefore { get; set; }
    public int? WhiteEloAfter { get; set; }
    public int? BlackEloAfter { get; set; }
}
