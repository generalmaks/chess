using Chess.Model.Chess.Game;
using Chess.Orchestrator.Game;

namespace Chess.Orchestrator.Tests.Game;

public class GetGameReviewAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task ReturnsReviewFromRepository()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        var id = Guid.NewGuid();
        repo.ReviewGame = new GameReview { Id = id, Result = GameResult.StalemateDraw, Moves = [] };

        var review = await orchestrator.GetGameReviewAsync(id);

        Assert.Equal(id, review.Id);
    }

    [Fact]
    public async Task ThrowsGameNotFoundWhenRepositoryReturnsNull()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        repo.ReviewGame = null;

        await Assert.ThrowsAsync<GameNotFoundException>(() => orchestrator.GetGameReviewAsync(Guid.NewGuid()));
    }
}
