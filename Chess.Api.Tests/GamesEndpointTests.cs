using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Chess.Api.Game;
using Chess.Api.Tests.Support;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;
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
            .Setup(o => o.CreateGameAsync(playerId, team, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room.ToGame());

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
            .Setup(o => o.CreateGameAsync(playerId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room.ToGame());

        var response = await client.PostAsync("/games", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>();
        Assert.Equal(room.Id, body!.GameId);
        Assert.Equal("White", body.Team);
    }

    [Fact]
    public async Task GameHistory_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/games/history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GameHistory_ForwardsPagingAndProjectsEntriesFromThePlayerPerspective()
    {
        var (client, playerId, orchestrator) = await AuthenticatedClientAsync();
        var opponentId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        orchestrator
            .Setup(o => o.GetPlayerGamesAsync(playerId, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<GameHistoryEntry>
            {
                Page = 2,
                PageSize = 5,
                TotalCount = 8,
                Items =
                [
                    new GameHistoryEntry
                    {
                        Id = gameId,
                        CreatedAtUtc = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
                        EndedAtUtc = new DateTime(2026, 8, 1, 12, 30, 0, DateTimeKind.Utc),
                        Result = GameResult.BlackWonByCheckmate,
                        WhitePlayerId = opponentId,
                        WhitePlayerName = "opponent",
                        BlackPlayerId = playerId,
                        BlackPlayerName = "me",
                        BlackEloBefore = 1200,
                        BlackEloAfter = 1215,
                        MoveCount = 42,
                    },
                ],
            });

        var response = await client.GetAsync("/games/history?page=2&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResultDto<GameHistoryEntryDto>>();
        Assert.Equal(2, body!.Page);
        Assert.Equal(8, body.TotalCount);
        Assert.Equal(2, body.TotalPages);

        var entry = Assert.Single(body.Items);
        Assert.Equal(gameId.ToString("N"), entry.GameId);
        Assert.Equal("Black", entry.PlayerColor);
        Assert.Equal("Win", entry.Outcome);
        Assert.Equal("opponent", entry.OpponentName);
        Assert.Equal(1200, entry.PlayerEloBefore);
        Assert.Equal(1215, entry.PlayerEloAfter);
        Assert.Equal(42, entry.MoveCount);
    }

    [Fact]
    public async Task GameReview_UnknownGame_ReturnsNotFound()
    {
        var (client, _, orchestrator) = await AuthenticatedClientAsync();
        orchestrator
            .Setup(o => o.GetGameReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GameNotFoundException());

        var response = await client.GetAsync($"/games/{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GameReview_ReturnsMovesAndPlayerColorFromThePlayerPerspective()
    {
        var (client, playerId, orchestrator) = await AuthenticatedClientAsync();
        var gameId = Guid.NewGuid();
        orchestrator
            .Setup(o => o.GetGameReviewAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameReview
            {
                Id = gameId,
                Result = GameResult.BlackWonByCheckmate,
                WhitePlayerId = Guid.NewGuid(),
                WhitePlayerName = "opponent",
                BlackPlayerId = playerId,
                BlackPlayerName = "me",
                Moves =
                [
                    new GameReviewMove { FromX = 4, FromY = 1, ToX = 4, ToY = 3, San = "e4" },
                    new GameReviewMove { FromX = 4, FromY = 6, ToX = 4, ToY = 4, San = "e5" },
                ],
            });

        var response = await client.GetAsync($"/games/{gameId:N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GameReviewDto>();
        Assert.Equal(gameId.ToString("N"), body!.GameId);
        Assert.Equal("Black", body.PlayerColor);
        Assert.Equal("opponent", body.WhitePlayerName);
        Assert.Equal(2, body.Moves.Length);
        Assert.Equal(4, body.Moves[0].FromX);
        Assert.Equal("e4", body.Moves[0].San);
    }

    private async Task<(HttpClient Client, Guid PlayerId, Mock<IGameOrchestrator> Orchestrator)> AuthenticatedClientAsync()
    {
        var (gameOrchestrator, playerOrchestrator) = factory.ResetMocks();
        var client = factory.CreateClient();
        var (token, playerId, _) = await AuthTestHelper.RegisterAsync(client, playerOrchestrator);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, playerId, gameOrchestrator);
    }
}
