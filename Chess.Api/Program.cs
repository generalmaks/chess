var builder = WebApplication.CreateBuilder(args);

var startup = Chess.Api.Program.StartupFactory(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app);

app.Run();

namespace Chess.Api
{
    public partial class Program
    {
        public static Func<IConfiguration, Startup> StartupFactory { get; set; } = config => new Startup(config);
    }
}
