using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

public sealed class InventorySchemaInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<InventorySchemaInitializer> logger) : BackgroundService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);
    private readonly HashSet<string> _migratedTenants = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inventory schema initializer started.");
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

        logger.LogInformation("Migrating Inventory databases for {Count} new tenant(s)...", pending.Count);
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

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
            await SeedInventoryDataAsync(dbContext, tenant.Code, cancellationToken);
            await TenantServiceReadiness.MarkReadyAsync(dbContext.Database.GetDbConnection(), cancellationToken);

            _migratedTenants.Add(tenant.Code);
            logger.LogInformation("Inventory database for tenant '{TenantCode}' migrated.", tenant.Code);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to migrate Inventory database for tenant '{TenantCode}'. Will retry.", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        InventoryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (appliedMigrations.Any()) return;

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pendingMigrations.Any()) return;

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var tableExistsCommand = connection.CreateCommand();
        tableExistsCommand.CommandText = "SELECT to_regclass('public.inventory_balances') IS NOT NULL;";
        if (await tableExistsCommand.ExecuteScalarAsync(cancellationToken) is not true) return;

        var firstPendingMigration = pendingMigrations.First();
        var efProductVersion = typeof(DbContext)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?.Split('+')[0] ?? "9.0.0";

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """);

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({firstPendingMigration}, {efProductVersion})
            ON CONFLICT ("MigrationId") DO NOTHING;
            """);
    }

    private async Task SeedInventoryDataAsync(
        InventoryDbContext dbContext,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        await InventoryDataSeeder.SeedAsync(dbContext, tenantCode, environment, logger, cancellationToken);
    }
}
