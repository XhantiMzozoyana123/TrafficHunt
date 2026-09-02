using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Infrastructure.Ai;
using TrafficHunt.Infrastructure.Persistence;
using TrafficHunt.Infrastructure.YouTube;

namespace TrafficHunt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
                // ---- Database (MySQL via Pomelo - Code First) ----
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Explicit MySQL 8.0 version avoids a live probe at design time.
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 39));

        services.AddDbContext<TrafficHuntDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));


        // ---- Repositories ----
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IProspectRepository, ProspectRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();

        // ---- YouTube discovery (YoutubeExplode) ----
        services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();

        // ---- YouTube Data API v3 (comment collection; OAuth replies in Milestone 2) ----
        services.AddHttpClient<IYouTubeApiService, YouTubeApiService>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ---- Ollama (local AI) ----
        services.AddHttpClient<IOllamaService, OllamaService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Ollama:BaseUrl"] ?? "http://localhost:11434");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        return services;
    }
}
