using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class FleetSchemaInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
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
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
            await SeedFleetDataAsync(dbContext, tenant.Code, cancellationToken);
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

    private async Task SeedFleetDataAsync(
        FleetDbContext dbContext,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        // 1. Create Vehicles
        // Vehicle 1: BRA2S19 (Truck)
        var v1 = await dbContext.Vehicles.Include(v => v.Assignments).IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Plate == "BRA2S19", cancellationToken);
        if (v1 == null)
        {
            v1 = Vehicle.Create("BRA2S19", "9BWZZZ99Z99999999", "123456789", "12345678", VehicleType.Truck, 12000m, 45m, new DateOnly(2027, 12, 31));
            
            // Driver Assignment: Marcos Oliveira (id: 33333333-3333-3333-3333-333333333333)
            var driverId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            v1.AssignDriver(driverId, new DateOnly(2026, 6, 10), null, "Alocação operacional padrão.");
            
            dbContext.Vehicles.Add(v1);
        }

        // Vehicle 2: XYZ8765 (Van)
        var v2 = await dbContext.Vehicles.Include(v => v.Assignments).IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Plate == "XYZ8765", cancellationToken);
        if (v2 == null)
        {
            v2 = Vehicle.Create("XYZ8765", "9BWZZZ88Z88888888", "987654321", "87654321", VehicleType.Van, 5000m, 20m, new DateOnly(2027, 12, 31));
            dbContext.Vehicles.Add(v2);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
