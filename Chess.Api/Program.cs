using Chess.Api;

var builder = WebApplication.CreateBuilder(args);

var startup = Program.StartupFactory(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app);

app.Run();

public partial class Program
{
    public static Func<IConfiguration, Startup> StartupFactory { get; set; } = config => new Startup(config);
}
