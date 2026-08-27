using Chess.Model.Chess.Game;
using Chess.Orchestrator.Game;
using Moq;

namespace Chess.Orchestrator.Tests.Game;

public class GetPlayerGamesAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task GetPlayerGamesAsync_ReturnsRequestedPageFromRepository()
    {
        var (orchestrator, repo) = CreateOrchestrator();
        for (var i = 0; i < 25; i++)
            repo.HistoryEntries.Add(Entry());

        var page = await orchestrator.GetPlayerGamesAsync(WhitePlayerId, page: 2, pageSize: 10);

        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(10, page.Items.Count);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public async Task GetPlayerGamesAsync_ClampsPageToAtLeastOne(int requested, int expected)
    {
        var (orchestrator, repo) = CreateOrchestrator();

        await orchestrator.GetPlayerGamesAsync(WhitePlayerId, requested, pageSize: 10);

        repo.Mock.Verify(r => r.GetPlayerGamesAsync(WhitePlayerId, expected, 10, It.IsAny<CancellationToken>()));
    }

    [Theory]
    [InlineData(0, GameOrchestrator.DefaultPageSize)]
    [InlineData(500, GameOrchestrator.MaxPageSize)]
    public async Task GetPlayerGamesAsync_ClampsPageSize(int requested, int expected)
    {
        var (orchestrator, repo) = CreateOrchestrator();

        await orchestrator.GetPlayerGamesAsync(WhitePlayerId, page: 1, requested);

        repo.Mock.Verify(r => r.GetPlayerGamesAsync(WhitePlayerId, 1, expected, It.IsAny<CancellationToken>()));
    }

    private static GameHistoryEntry Entry() => new()
    {
        Id = Guid.NewGuid(),
        CreatedAtUtc = DateTime.UtcNow,
        Result = GameResult.Ongoing,
        WhitePlayerId = WhitePlayerId,
        BlackPlayerId = BlackPlayerId,
        MoveCount = 0,
    };
}
