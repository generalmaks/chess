using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chess.Api.Contracts;
using Chess.Api.Tests.Support;
using Chess.Logic.Pieces.Chess;
using Chess.Orchestrator;
using Moq;

namespace Chess.Api.Tests;

public class GamesEndpointTests(ChessApiFactory factory) : IClassFixture<ChessApiFactory>
{
    [Fact]
    public async Task CreateGame_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/games", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("white", "White")]
    [InlineData("black", "Black")]
    public async Task CreateGame_ColorQueryParam_IsForwardedToOrchestrator(string colorParam, string expectedTeam)
    {
        var (client, playerId, orchestrator) = await AuthenticatedClientAsync();
        var team = Enum.Parse<Team>(expectedTeam);
        var room = new GameRoom(Guid.NewGuid().ToString());
        room.SeatCreator(playerId, team);
        orchestrator
            .Setup(o => o.CreateGameAsync(playerId, team, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var response = await client.PostAsync($"/games?color={colorParam}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>();
        Assert.Equal(room.Id, body!.GameId);
        Assert.Equal(expectedTeam, body.Team);
    }

    [Fact]
    public async Task CreateGame_NoColorQueryParam_ReturnsOkWithValidResponseShape()
    {
        var (client, playerId, orchestrator) = await AuthenticatedClientAsync();
        var room = new GameRoom(Guid.NewGuid().ToString());
        room.SeatCreator(playerId, Team.White);
        orchestrator
            .Setup(o => o.CreateGameAsync(playerId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var response = await client.PostAsync("/games", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>();
        Assert.Equal(room.Id, body!.GameId);
        Assert.Equal("White", body.Team);
    }

    private async Task<(HttpClient Client, Guid PlayerId, Mock<IGameOrchestrator> Orchestrator)> AuthenticatedClientAsync()
    {
        var (orchestrator, authenticator) = factory.ResetMocks();
        var client = factory.CreateClient();
        var (token, playerId, _) = await AuthTestHelper.RegisterAsync(client, authenticator);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, playerId, orchestrator);
    }
}
