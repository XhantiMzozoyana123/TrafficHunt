using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Infrastructure.Ai;
using TrafficHunt.Infrastructure.Jobs;
using TrafficHunt.Infrastructure.Persistence;
using TrafficHunt.Infrastructure.Persistence.Repositories;
using TrafficHunt.Infrastructure.YouTube;

namespace TrafficHunt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- Database (MySQL via Pomelo - Code First) ----
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 39));

        services.AddDbContext<TrafficHuntDbContext>(options =>
            options.UseMySql(connectionString, serverVersion,
                mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        // ---- Repositories ----
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IProspectRepository, ProspectRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IReplyCampaignRepository, ReplyCampaignRepository>();

        // ---- YouTube discovery (YoutubeExplode) ----
        services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();

        // ---- YouTube Data API v3 (comment scraping) ----
        services.AddHttpClient<IYouTubeApiService, YouTubeApiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ---- YouTube reply publishing: official Google APIs client (Data API v3) ----
        services.AddHttpClient<IYouTubeReplySender, GoogleReplySender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ---- Ollama (AI) ----
        // Long timeout: the remote LLM cold-loads the 6.6GB qwen3.5 model
        // (60s+ on first hit) and comment qualification retries once inside
        // OllamaService, so fail-fast here would cause false AI failures.
        // HTTP requests never block on the LLM anyway — all AI work runs in
        // Hangfire jobs (ai queue, single worker) — except AI campaign
        // planning, which is also chunk-safe (one LLM call).
        // Ollama:TimeoutSeconds (env Ollama__TimeoutSeconds) overrides this.
        var ollamaTimeout = TimeSpan.FromSeconds(
            configuration.GetValue<int?>("Ollama:TimeoutSeconds") ?? 300);
        services.AddHttpClient<IOllamaService, OllamaService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Ollama:BaseUrl"] ?? "http://localhost:11434");
            client.Timeout = ollamaTimeout;
        });

        // ---- Hangfire background jobs ----
        services.AddScoped<NotificationJob>();

        return services;
    }
}
