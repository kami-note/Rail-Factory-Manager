using Microsoft.EntityFrameworkCore;
using RailFactory.Inventory.Api.Application;
using RailFactory.Inventory.Api.Application.Balances;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Infrastructure.Persistence;

namespace RailFactory.Inventory.Api.Infrastructure;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.ResolveConnection("inventorydb");
            options.UseNpgsql(connectionString);
        });

        services.AddHostedService<InventorySchemaInitializer>();
        services.AddScoped<IInventoryRepository, PostgresInventoryRepository>();

        services.AddScoped<GetInventoryInfo>();
        services.AddScoped<CreatePendingBalance>();
        services.AddScoped<ListPendingBalances>();

        return services;
    }
}
