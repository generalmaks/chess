using Chess.Api.Contracts;
using Chess.Api.Hubs;
using Chess.Dal;
using Chess.Orchestrator;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string devCorsPolicy = "DevCors";

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameStore>();
builder.Services.AddScoped<GameOrchestrator>();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(devCorsPolicy);
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/games", async (GameOrchestrator orchestrator) =>
    {
        var room = await orchestrator.CreateGameAsync();
        return new CreateGameResponse(room.Id, room.WhiteToken, room.BlackToken);
    })
    .WithName("CreateGame");

app.MapHub<ChessHub>("/hubs/chess");

app.Run();
