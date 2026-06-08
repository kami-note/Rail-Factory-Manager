using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class FleetSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<FleetSchemaInitializer> logger) : BackgroundService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);
    private readonly HashSet<string> _migratedTenants = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Fleet schema initializer started.");
        await MigrateNewTenantsAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await MigrateNewTenantsAsync(stoppingToken);
    }

    private async Task MigrateNewTenantsAsync(CancellationToken cancellationToken)
    {
        List<TenantResolutionResult> pending;
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
            var all = await client.ListAllAsync(cancellationToken);
            pending = all.Where(t => t.IsActive && !_migratedTenants.Contains(t.Code)).ToList();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Could not fetch tenant list from catalog. Will retry in 15s.");
            return;
        }

        if (pending.Count == 0) return;

        logger.LogInformation("Migrating Fleet databases for {Count} new tenant(s)...", pending.Count);
        await Task.WhenAll(pending.Select(t => MigrateTenantAsync(t, cancellationToken)));
    }

    private async Task MigrateTenantAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        await MigrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            using var tenantScope = serviceProvider.CreateScope();
            var scopedContextAccessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            scopedContextAccessor.Current = new TenantContext(
                tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<FleetDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
            await TenantServiceReadiness.MarkReadyAsync(dbContext.Database.GetDbConnection(), cancellationToken);

            _migratedTenants.Add(tenant.Code);
            logger.LogInformation("Fleet database for tenant '{TenantCode}' migrated.", tenant.Code);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to migrate Fleet database for tenant '{TenantCode}'. Will retry.", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }
}
