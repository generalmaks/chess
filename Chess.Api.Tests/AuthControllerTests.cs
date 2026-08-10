using System.Net;
using System.Net.Http.Json;
using Chess.Api.Contracts;
using Chess.Api.Tests.Support;
using Chess.Orchestrator;
using Moq;

namespace Chess.Api.Tests;

public class AuthControllerTests(ChessApiFactory factory) : IClassFixture<ChessApiFactory>
{
    [Fact]
    public async Task Register_Success_ReturnsTokenAndUsername()
    {
        var (_, authenticator) = factory.ResetMocks();
        authenticator
            .Setup(a => a.RegisterAsync("newplayer", "password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticatedPlayer(Guid.NewGuid(), "newplayer"));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("newplayer", "password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("newplayer", body.Username);
    }

    [Theory]
    [InlineData(typeof(UsernameTakenException))]
    [InlineData(typeof(InvalidUsernameException))]
    [InlineData(typeof(WeakPasswordException))]
    public async Task Register_AuthException_ReturnsBadRequest(Type exceptionType)
    {
        var (_, authenticator) = factory.ResetMocks();
        authenticator
            .Setup(a => a.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync((AuthException)Activator.CreateInstance(exceptionType)!);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("whoever", "whatever1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_Success_ReturnsToken()
    {
        var (_, authenticator) = factory.ResetMocks();
        authenticator
            .Setup(a => a.LoginAsync("player", "password123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticatedPlayer(Guid.NewGuid(), "player"));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("player", "password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var (_, authenticator) = factory.ResetMocks();
        authenticator
            .Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidCredentialsException());
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(AuthTestHelper.RandomUsername(), "wrongPassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUsername_ReturnsUnauthorized()
    {
        var (_, authenticator) = factory.ResetMocks();
        authenticator
            .Setup(a => a.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidCredentialsException());
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest(AuthTestHelper.RandomUsername(), "password123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
