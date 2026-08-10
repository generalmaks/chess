using Chess.Orchestrator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chess.Api.Tests.Support;

public class TestStartup(IConfiguration configuration) : Startup(configuration)
{
    protected override void ConfigureRepositories(IServiceCollection services)
    {
    }

    protected override void ConfigureOrchestration(IServiceCollection services)
    {
        services.AddSingleton<ChessApiFactory.Switchboard>();
        services.AddScoped<IGameOrchestrator>(sp => sp.GetRequiredService<ChessApiFactory.Switchboard>().GameOrchestrator);
        services.AddScoped<IPlayerAuthenticator>(sp => sp.GetRequiredService<ChessApiFactory.Switchboard>().Authenticator);
    }
}
