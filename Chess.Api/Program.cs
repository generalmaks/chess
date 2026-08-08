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

var builder = WebApplication.CreateBuilder(args);

const string devCorsPolicy = "DevCors";
const string chessHubPath = "/hubs/chess";

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameStore>();
builder.Services.AddScoped<GameOrchestrator>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<IPlayerAuthenticator, PlayerAuthenticator>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenFactory>();
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Chess")));
builder.Services.AddCors(options =>
{
    options.AddPolicy(devCorsPolicy, policy => policy
        .WithOrigins("http://localhost:3000", "https://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, the handler remaps short claim names (e.g. "sub") to long
        // ClaimTypes URIs, so FindFirstValue(JwtRegisteredClaimNames.Sub) never matches.
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

        // Browsers can't set an Authorization header on the WebSocket handshake SignalR
        // uses, so the client passes the JWT as a query string param instead.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments(chessHubPath))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseHttpsRedirection();
app.UseCors(devCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.MapPost("/games", async (GameOrchestrator orchestrator, ClaimsPrincipal user, string? color) =>
    {
        var playerId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var preferredTeam = ParseColorPreference(color);

        var room = await orchestrator.CreateGameAsync(playerId, preferredTeam);
        var assignedTeam = room.TeamForPlayer(playerId)!.Value;

        return new CreateGameResponse(room.Id, assignedTeam.ToString());
    })
    .RequireAuthorization()
    .WithName("CreateGame");

app.MapHub<ChessHub>(chessHubPath);

app.Run();

// "white"/"black" pick that team; anything else (including omitted/empty) means no
// preference, so CreateGameAsync rolls a coin for the creator's seat.
static Team? ParseColorPreference(string? color) => color?.Trim().ToLowerInvariant() switch
{
    "white" => Team.White,
    "black" => Team.Black,
    _ => null,
};
