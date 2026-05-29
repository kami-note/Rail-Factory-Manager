using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

/// <summary>
/// Initializes the Fleet database schema for all active tenants on startup.
/// </summary>
public sealed class FleetSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<FleetSchemaInitializer> logger) : IHostedService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Fleet multi-tenant schema initialization...");

        await using var scope = serviceProvider.CreateAsyncScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();

        var allTenants = await catalogClient.ListAllAsync(cancellationToken);
        var activeTenants = allTenants.Where(t => t.Found && t.IsActive).ToList();

        if (!activeTenants.Any())
        {
            logger.LogWarning("No active tenants found in catalog for Fleet migration.");
            return;
        }

        var migrationTasks = activeTenants.Select(tenant => MigrateTenantAsync(tenant, cancellationToken));
        await Task.WhenAll(migrationTasks);

        logger.LogInformation("Fleet multi-tenant schema initialization completed.");
    }

    private async Task MigrateTenantAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        await MigrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            logger.LogInformation("Migrating Fleet database for tenant: {TenantCode}", tenant.Code);

            using var tenantScope = serviceProvider.CreateScope();
            var scopedContextAccessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            scopedContextAccessor.Current = new TenantContext(
                tenant.Code,
                tenant.Locale,
                tenant.TimeZone,
                tenant.ConnectionStrings);

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<FleetDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Fleet database for tenant {TenantCode} initialized successfully.", tenant.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to migrate Fleet database for tenant: {TenantCode}", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
