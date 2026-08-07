using Chess.Api.Auth;
using Chess.Api.Contracts;
using Chess.Orchestrator;
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
            return Ok(new AuthResponse(tokens.CreateToken(player), player.Username));
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
            return Ok(new AuthResponse(tokens.CreateToken(player), player.Username));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized();
        }
    }
}
