using RailFactory.Inventory.Api.Application;

namespace RailFactory.Inventory.Api.Infrastructure;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<GetInventoryInfo>();
        return services;
    }
}
