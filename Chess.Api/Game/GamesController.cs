using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Chess.Model.Chess.Game;
using Chess.Model.Chess.Pieces.Chess;
using Chess.Orchestrator.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chess.Api.Game;

[Authorize]
[ApiController]
[Route("games")]
public class GamesController(IGameOrchestrator orchestrator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateGameResponse>> Create(string? color, int? initialMinutes, int? incrementSeconds)
    {
        var preferredTeam = ParseColorPreference(color);
        var timeControl = ParseTimeControl(initialMinutes, incrementSeconds);

        var room = await orchestrator.CreateGameAsync(PlayerId, preferredTeam, timeControl);
        var assignedTeam = room.TeamForPlayer(PlayerId)!.Value;

        return Ok(new CreateGameResponse(room.Id, assignedTeam.ToString()));
    }

    [HttpGet("history")]
    public async Task<ActionResult<PagedResultDto<GameHistoryEntryDto>>> History(int? page, int? pageSize, CancellationToken ct)
    {
        var result = await orchestrator.GetPlayerGamesAsync(PlayerId, page ?? 1, pageSize ?? 20, ct);
        return Ok(GameStateMapper.ToHistoryDto(result, PlayerId));
    }

    [HttpGet("{gameId}")]
    public async Task<ActionResult<GameReviewDto>> Review(string gameId, CancellationToken ct)
    {
        if (!Guid.TryParse(gameId, out var id))
            throw new GameNotFoundException();

        var review = await orchestrator.GetGameReviewAsync(id, ct);
        return Ok(GameStateMapper.ToReviewDto(review, PlayerId));
    }

    private Guid PlayerId => Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static Team? ParseColorPreference(string? color) => color?.Trim().ToLowerInvariant() switch
    {
        "white" => Team.White,
        "black" => Team.Black,
        _ => null,
    };

    private static TimeControl? ParseTimeControl(int? initialMinutes, int? incrementSeconds) =>
        initialMinutes is null ? null : TimeControl.Create(TimeSpan.FromMinutes(initialMinutes.Value), TimeSpan.FromSeconds(incrementSeconds ?? 0));
}
