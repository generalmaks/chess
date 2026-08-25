using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;

namespace Chess.Orchestrator.Tests.Game;

public class OfferDrawTests : GameOrchestratorTestBase
{
    [Fact]
    public void OfferDraw_WithoutJoiningFirst_ThrowsNoActiveConnectionException()
    {
        var (orchestrator, _) = CreateOrchestrator();

        Assert.Throws<NoActiveConnectionException>(() => orchestrator.OfferDraw("conn-1"));
    }

    [Fact]
    public async Task OfferDraw_RecordsOfferingTeamOnRoom()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var room = await orchestrator.CreateGameAsync(WhitePlayerId, Team.White);
        await orchestrator.JoinAsync("black-conn", room.Id, BlackPlayerId);

        var (offeringRoom, team) = orchestrator.OfferDraw("black-conn");

        Assert.Equal(Team.Black, team);
        Assert.Equal(Team.Black, offeringRoom.DrawOfferedBy);
    }
}
