using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TrafficHunt.Application;
using TrafficHunt.Infrastructure;

namespace TrafficHunt.Mcp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureAppConfiguration(cfg =>
        {
            cfg.AddJsonFile("appsettings.json", optional: true);
            cfg.AddEnvironmentVariables();
        });
        builder.ConfigureServices((context, services) =>
        {
            services.AddApplication();
            services.AddInfrastructure(context.Configuration);
            services.AddSingleton<McpToolHandler>();
        });

        var host = builder.Build();

        var handler = host.Services.GetRequiredService<McpToolHandler>();
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await handler.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput(), cts.Token);
    }
}

