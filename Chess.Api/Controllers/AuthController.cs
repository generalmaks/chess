using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chess.Api.Auth;
using Chess.Api.Contracts;
using Chess.Orchestrator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chess.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IPlayerAuthenticator authenticator, JwtTokenFactory tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        try
        {
            var player = await authenticator.RegisterAsync(request.Username, request.Password);
            return Ok(new AuthResponse(tokens.CreateToken(player), player.Username, player.EloRating));
        }
        catch (AuthException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        try
        {
            var player = await authenticator.LoginAsync(request.Username, request.Password);
            return Ok(new AuthResponse(tokens.CreateToken(player), player.Username, player.EloRating));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized();
        }
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        var playerId = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        try
        {
            var player = await authenticator.RefreshAsync(playerId);
            return Ok(new AuthResponse(tokens.CreateToken(player), player.Username, player.EloRating));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized();
        }
    }
}
