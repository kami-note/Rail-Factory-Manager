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
        var inventoryConnectionString = ResolveInventoryConnectionString(configuration)
            ?? throw new InvalidOperationException(
                "Inventory database connection string is required. Configure ConnectionStrings:inventorydb or ConnectionStrings:tenant-dev-inventorydb.");

        services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(inventoryConnectionString));
        services.AddHostedService<InventorySchemaInitializer>();
        services.AddScoped<IInventoryRepository, PostgresInventoryRepository>();

        services.AddScoped<GetInventoryInfo>();
        services.AddScoped<CreatePendingBalance>();
        services.AddScoped<ListPendingBalances>();

        return services;
    }

    private static string? ResolveInventoryConnectionString(IConfiguration configuration)
    {
        var primary = configuration.GetConnectionString("inventorydb");
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        return configuration.GetConnectionString("tenant-dev-inventorydb");
    }
}
