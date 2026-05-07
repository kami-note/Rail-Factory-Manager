using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

/// <summary>
/// Initializes the Inventory database schema for all active tenants.
/// </summary>
/// <remarks>
/// This initializer iterates over all tenants in the catalog and applies EF Core migrations.
/// It uses a <see cref="SemaphoreSlim"/> to limit concurrent database migrations,
/// ensuring stability and preventing connection pool exhaustion during startup.
/// Resilience is handled by the underlying <see cref="ITenantCatalogClient"/>'s resilience policies.
/// </remarks>
public sealed class InventorySchemaInitializer(IServiceProvider serviceProvider, ILogger<InventorySchemaInitializer> logger) : IHostedService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Inventory multi-tenant schema initialization...");

        await using var scope = serviceProvider.CreateAsyncScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();

        var allTenants = await catalogClient.ListAllAsync(cancellationToken);
        var activeTenants = allTenants.Where(t => t.Found && t.IsActive).ToList();

        if (!activeTenants.Any())
        {
            logger.LogWarning("No active tenants found in catalog for Inventory migration.");
            return;
        }

        var migrationTasks = activeTenants.Select(tenant => MigrateTenantAsync(tenant, cancellationToken));
        await Task.WhenAll(migrationTasks);

        logger.LogInformation("Inventory multi-tenant schema initialization completed.");
    }

    private async Task MigrateTenantAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        await MigrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            logger.LogInformation("Migrating Inventory database for tenant: {TenantCode}", tenant.Code);

            using var tenantScope = serviceProvider.CreateScope();
            var scopedContextAccessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            scopedContextAccessor.Current = new TenantContext(
                tenant.Code,
                tenant.Locale,
                tenant.TimeZone,
                tenant.ConnectionStrings);

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            await dbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Inventory database for tenant {TenantCode} initialized successfully.", tenant.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to migrate Inventory database for tenant: {TenantCode}", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
