using RailFactory.SupplyChain.Api.Application;

namespace RailFactory.SupplyChain.Api.Infrastructure;

public static class SupplyChainModule
{
    public static IServiceCollection AddSupplyChainModule(this IServiceCollection services)
    {
        services.AddScoped<GetSupplyChainInfo>();
        return services;
    }
}
