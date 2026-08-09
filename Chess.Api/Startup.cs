using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Chess.Api.Auth;
using Chess.Api.Contracts;
using Chess.Api.Hubs;
using Chess.Dal;
using Chess.Logic.Pieces.Chess;
using Chess.Orchestrator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Chess.Api;

public class Startup(IConfiguration configuration)
{
    public const string DevCorsPolicy = "DevCors";
    public const string ChessHubPath = "/hubs/chess";

    protected IConfiguration Configuration { get; } = configuration;

    public virtual void ConfigureServices(IServiceCollection services)
    {
        var jwtOptions = Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSignalR();
        services.Configure<JwtOptions>(Configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtTokenFactory>();

        ConfigureRepositories(services);
        ConfigureOrchestration(services);

        services.AddCors(options =>
        {
            options.AddPolicy(DevCorsPolicy, policy => policy
                .WithOrigins("http://localhost:3000", "https://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(ChessHubPath))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();
    }
    
    protected virtual void ConfigureRepositories(IServiceCollection services)
    {
        services.AddDbContext<ChessDbContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString("Chess")));
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
    }

    protected virtual void ConfigureOrchestration(IServiceCollection services)
    {
        services.AddSingleton<GameStore>();
        services.AddScoped<IGameOrchestrator, GameOrchestrator>();
        services.AddScoped<IPlayerAuthenticator, PlayerAuthenticator>();
    }

    public virtual void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseCors(DevCorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapPost("/games", async (IGameOrchestrator orchestrator, ClaimsPrincipal user, string? color) =>
            {
                var playerId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
                var preferredTeam = ParseColorPreference(color);

                var room = await orchestrator.CreateGameAsync(playerId, preferredTeam);
                var assignedTeam = room.TeamForPlayer(playerId)!.Value;

                return new CreateGameResponse(room.Id, assignedTeam.ToString());
            })
            .RequireAuthorization()
            .WithName("CreateGame");

        app.MapHub<ChessHub>(ChessHubPath);
    }
    
    private static Team? ParseColorPreference(string? color) => color?.Trim().ToLowerInvariant() switch
    {
        "white" => Team.White,
        "black" => Team.Black,
        _ => null,
    };
}
