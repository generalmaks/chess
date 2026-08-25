using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Orchestrator.Tests.Game;

public class CreateGameAsyncTests : GameOrchestratorTestBase
{
    [Fact]
    public async Task CreateGameAsync_PreferredWhite_SeatsCreatorAsWhiteAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();

        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);

        Assert.Equal(WhitePlayerId, room.WhitePlayerId);
        Assert.Null(room.BlackPlayerId);

        var added = Assert.Single(repo.AddedGames);
        Assert.Equal(GameResult.Ongoing, added.Result);
        Assert.Null(added.EndedAtUtc);
        Assert.Equal(WhitePlayerId, added.WhitePlayerId);
        Assert.Null(added.BlackPlayerId);
    }

    [Fact]
    public async Task CreateGameAsync_PreferredBlack_SeatsCreatorAsBlackAndPersists()
    {
        var (orchestrator, repo) = CreateOrchestrator();

        var room = await orchestrator.CreateGameAsync(BlackPlayerId, Team.Black);

        Assert.Equal(BlackPlayerId, room.BlackPlayerId);
        Assert.Null(room.WhitePlayerId);

        var added = Assert.Single(repo.AddedGames);
        Assert.Equal(BlackPlayerId, added.BlackPlayerId);
        Assert.Null(added.WhitePlayerId);
    }

    [Fact]
    public async Task CreateGameAsync_NoPreference_SeatsCreatorInExactlyOneRandomlyChosenTeam()
    {
        var (orchestrator, _) = CreateOrchestrator();

        var sawWhite = false;
        var sawBlack = false;

        // Random assignment is a coin flip; run enough trials that both outcomes are
        // overwhelmingly likely to show up so this isn't a flaky test.
        for (var i = 0; i < 50; i++)
        {
            var room = await orchestrator.CreateGameAsync(Guid.NewGuid());

            // Exactly one seat should be filled - never both, never neither.
            Assert.NotEqual(room.WhitePlayerId is null, room.BlackPlayerId is null);
            sawWhite |= room.WhitePlayerId is not null;
            sawBlack |= room.BlackPlayerId is not null;
        }

        Assert.True(sawWhite);
        Assert.True(sawBlack);
    }
}
