using System.Net.Http.Json;
using Chess.Api.Contracts;
using Chess.Orchestrator;
using Moq;

namespace Chess.Api.Tests.Support;

public static class AuthTestHelper
{
    public static string RandomUsername() => $"player-{Guid.NewGuid():N}"[..20];

    public static async Task<(string Token, Guid PlayerId, string Username)> RegisterAsync(
        HttpClient client,
        Mock<IPlayerAuthenticator> authenticator,
        string? username = null,
        string password = "password123")
    {
        username ??= RandomUsername();
        var playerId = Guid.NewGuid();
        authenticator
            .Setup(a => a.RegisterAsync(username, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticatedPlayer(playerId, username));

        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(username, password));
        if (!response.IsSuccessStatusCode)
            throw new Exception($"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        return (body.Token, playerId, username);
    }
}
