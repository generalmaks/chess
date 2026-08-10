using Chess.Api.Contracts;
using Chess.Api.Tests.Support;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;
using Chess.Orchestrator;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Moq;

namespace Chess.Api.Tests;

public class ChessHubTests(ChessApiFactory factory) : IClassFixture<ChessApiFactory>, IAsyncLifetime
{
    private readonly List<HubConnection> _connections = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var connection in _connections)
            await connection.DisposeAsync();
    }

    [Fact]
    public async Task JoinGame_Creator_ReturnsCreatorsSeatedTeam()
    {
        var (orchestrator, authenticator) = factory.ResetMocks();
        var (token, playerId, _) = await AuthTestHelper.RegisterAsync(factory.CreateClient(), authenticator);
        var gameId = Guid.NewGuid().ToString();
        var room = new GameRoom(gameId);
        orchestrator
            .Setup(o => o.JoinAsync(It.IsAny<string>(), gameId, playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((room, Team.White));

        var connection = await ConnectAsync(token);
        var joined = await connection.InvokeAsync<JoinGameResponse>("JoinGame", gameId);

        Assert.Equal("White", joined.Team);
    }

    [Fact]
    public async Task JoinGame_SecondPlayer_ClaimsRemainingSeat()
    {
        var (orchestrator, authenticator) = factory.ResetMocks();
        var client = factory.CreateClient();
        var gameId = Guid.NewGuid().ToString();
        var room = new GameRoom(gameId);

        var (creatorToken, creatorId, _) = await AuthTestHelper.RegisterAsync(client, authenticator);
        var (joinerToken, joinerId, _) = await AuthTestHelper.RegisterAsync(client, authenticator);
        orchestrator.Setup(o => o.JoinAsync(It.IsAny<string>(), gameId, creatorId, It.IsAny<CancellationToken>())).ReturnsAsync((room, Team.White));
        orchestrator.Setup(o => o.JoinAsync(It.IsAny<string>(), gameId, joinerId, It.IsAny<CancellationToken>())).ReturnsAsync((room, Team.Black));

        var creatorConnection = await ConnectAsync(creatorToken);
        await creatorConnection.InvokeAsync<JoinGameResponse>("JoinGame", gameId);

        var joinerConnection = await ConnectAsync(joinerToken);
        var joined = await joinerConnection.InvokeAsync<JoinGameResponse>("JoinGame", gameId);

        Assert.Equal("Black", joined.Team);
    }

    [Fact]
    public async Task JoinGame_ThirdPlayer_ThrowsHubException()
    {
        var (orchestrator, authenticator) = factory.ResetMocks();
        var (token, playerId, _) = await AuthTestHelper.RegisterAsync(factory.CreateClient(), authenticator);
        var gameId = Guid.NewGuid().ToString();
        orchestrator
            .Setup(o => o.JoinAsync(It.IsAny<string>(), gameId, playerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GameFullException());

        var connection = await ConnectAsync(token);

        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<JoinGameResponse>("JoinGame", gameId));
    }

    [Fact]
    public async Task Connect_WithoutToken_IsRejected()
    {
        var connection = BuildConnection(accessToken: null);
        _connections.Add(connection);

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task MakeMove_LegalMove_BroadcastsUpdatedStateWithSwitchedTurn()
    {
        var (orchestrator, authenticator) = factory.ResetMocks();
        var client = factory.CreateClient();
        var gameId = Guid.NewGuid().ToString();
        var room = new GameRoom(gameId);

        var (whiteToken, whiteId, _) = await AuthTestHelper.RegisterAsync(client, authenticator);
        var (blackToken, blackId, _) = await AuthTestHelper.RegisterAsync(client, authenticator);
        orchestrator.Setup(o => o.JoinAsync(It.IsAny<string>(), gameId, whiteId, It.IsAny<CancellationToken>())).ReturnsAsync((room, Team.White));
        orchestrator.Setup(o => o.JoinAsync(It.IsAny<string>(), gameId, blackId, It.IsAny<CancellationToken>())).ReturnsAsync((room, Team.Black));

        var whiteConnection = await ConnectAsync(whiteToken);
        await whiteConnection.InvokeAsync<JoinGameResponse>("JoinGame", gameId);

        var blackConnection = await ConnectAsync(blackToken);
        await blackConnection.InvokeAsync<JoinGameResponse>("JoinGame", gameId);

        room.Session.MakeMove(new PieceMove(new PieceCord(4, 1), new PieceCord(4, 3)), null);
        orchestrator
            .Setup(o => o.MakeMoveAsync(It.IsAny<string>(), It.IsAny<PieceMove>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var stateUpdated = new TaskCompletionSource<GameStateDto>();
        blackConnection.On<GameStateDto>("StateUpdated", state => stateUpdated.TrySetResult(state));

        await whiteConnection.InvokeAsync("MakeMove", new MoveRequest(4, 1, 4, 3, null));

        var updated = await stateUpdated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Black", updated.CurrentTurn);
    }

    private async Task<HubConnection> ConnectAsync(string accessToken)
    {
        var connection = BuildConnection(accessToken);
        _connections.Add(connection);
        await connection.StartAsync();
        return connection;
    }

    private HubConnection BuildConnection(string? accessToken) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/hubs/chess"), HttpTransportType.LongPolling, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                if (accessToken is not null)
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();
}
