using Microsoft.Extensions.DependencyInjection;
using TrafficHunt.Application.Interfaces;
using TrafficHunt.Application.Services;

namespace TrafficHunt.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IProspectService, ProspectService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        return services;
    }
}
