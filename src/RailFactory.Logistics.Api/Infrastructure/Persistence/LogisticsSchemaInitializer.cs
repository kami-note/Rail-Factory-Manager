using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class LogisticsSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<LogisticsSchemaInitializer> logger) : IHostedService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Logistics multi-tenant schema initialization...");

        await using var scope = serviceProvider.CreateAsyncScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();

        var allTenants = await catalogClient.ListAllAsync(cancellationToken);
        var activeTenants = allTenants.Where(t => t.Found && t.IsActive).ToList();

        if (!activeTenants.Any())
        {
            logger.LogWarning("No active tenants found in catalog for Logistics migration.");
            return;
        }

        var migrationTasks = activeTenants.Select(tenant => MigrateTenantAsync(tenant, cancellationToken));
        await Task.WhenAll(migrationTasks);

        logger.LogInformation("Logistics multi-tenant schema initialization completed.");
    }

    private async Task MigrateTenantAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        await MigrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            logger.LogInformation("Migrating Logistics database for tenant: {TenantCode}", tenant.Code);

            using var tenantScope = serviceProvider.CreateScope();
            var scopedContextAccessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            scopedContextAccessor.Current = new TenantContext(
                tenant.Code,
                tenant.Locale,
                tenant.TimeZone,
                tenant.ConnectionStrings);

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<LogisticsDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("Logistics database for tenant {TenantCode} initialized successfully.", tenant.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to migrate Logistics database for tenant: {TenantCode}", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
