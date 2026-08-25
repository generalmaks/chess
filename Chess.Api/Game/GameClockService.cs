using Chess.Api.Contracts;
using Chess.Api.Hubs;
using Chess.Logic;
using Chess.Orchestrator;
using Microsoft.AspNetCore.SignalR;

namespace Chess.Api.Services;

public class GameClockService(IServiceScopeFactory scopeFactory, GameStore store, IHubContext<ChessHub> hubContext) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var room in store.ActiveTimedGames)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IGameOrchestrator>();
                var endedGame = await orchestrator.CheckTimeoutAsync(room.Id, stoppingToken);
                if (endedGame is null)
                    continue;

                await hubContext.Clients.Group(ChessHub.GroupName(endedGame.Id))
                    .SendAsync("StateUpdated", GameStateMapper.ToDto(endedGame.State), stoppingToken);
            }
        }
    }
}
