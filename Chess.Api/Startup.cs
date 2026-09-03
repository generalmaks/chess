using System.Text;
using Chess.Api.Game;
using Chess.Api.Middleware;
using Chess.Api.Player;
using Chess.Dal;
using Chess.Dal.Chess;
using Chess.Dal.Player;
using Chess.Model.Chess.Game;
using Chess.Model.Player;
using Chess.Orchestrator.Game;
using Chess.Orchestrator.Player;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

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
        services.AddSwaggerGen(options =>
        {
            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the JWT from /auth/login or /auth/register.",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            };

            options.AddSecurityDefinition("Bearer", scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
        });
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
        services.AddScoped<IPlayerOrchestrator, PlayerOrchestrator>();
        services.AddHostedService<GameClockService>();
    }

    public virtual void Configure(WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseSerilogRequestLogging();

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

        app.MapHub<ChessHub>(ChessHubPath);
    }
}
