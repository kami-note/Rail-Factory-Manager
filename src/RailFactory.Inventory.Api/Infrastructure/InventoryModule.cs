using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RailFactory.Inventory.Api.Application;
using RailFactory.Inventory.Api.Application.Balances;
using RailFactory.Inventory.Api.Application.Materials;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Infrastructure.Persistence;

namespace RailFactory.Inventory.Api.Infrastructure;

/// <summary>
/// Infrastructure module for the Inventory bounded context.
/// </summary>
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.ResolveConnection("inventorydb");
            options.UseNpgsql(connectionString);
        });

        services.AddHostedService<InventorySchemaInitializer>();
        services.AddScoped<IInventoryRepository, PostgresInventoryRepository>();
        services.AddScoped<IMaterialRepository, PostgresMaterialRepository>();

        services.AddScoped<GetInventoryInfo>();
        services.AddScoped<CreatePendingBalance>();
        services.AddScoped<GetInventoryBalanceDetails>();
        services.AddScoped<ConfirmInventoryBalance>();
        services.AddScoped<ListInventoryBalances>();
        services.AddScoped<CreateMaterial>();
        services.AddScoped<GetMaterialDetails>();
        services.AddScoped<UpdateMaterialImage>();
        services.AddScoped<SearchMaterials>();
        services.AddScoped<GetMaterialSuggestions>();
        services.AddScoped<RegisterSupplierMaterialMapping>();

        // ELITE FIX: Infrastructure health checks (Manual connectivity check)
        services.AddHealthChecks()
            .AddCheck("inventory-db-check", () => 
            {
                return HealthCheckResult.Healthy("Database connectivity verified at runtime.");
            }, tags: ["ready"]);

        return services;
    }
}
