using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chess.Model.Player;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chess.Api.Player;

[ApiController]
[Route("auth")]
public class AuthController(IPlayerOrchestrator orchestrator, JwtTokenFactory tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var player = await orchestrator.RegisterAsync(request.Username, request.Password);
        return Ok(new AuthResponse(tokens.CreateToken(player), player.Username, player.EloRating));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var player = await orchestrator.LoginAsync(request.Username, request.Password);
        return Ok(new AuthResponse(tokens.CreateToken(player), player.Username, player.EloRating));
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        var playerId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        var player = await orchestrator.RefreshAsync(playerId);
        return Ok(new AuthResponse(tokens.CreateToken(player), player.Username, player.EloRating));
    }
}
