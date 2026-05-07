using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using System.Reflection;

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

            await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
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

    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        InventoryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (appliedMigrations.Any())
        {
            return;
        }

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pendingMigrations.Any())
        {
            return;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var tableExistsCommand = connection.CreateCommand();
        tableExistsCommand.CommandText = "SELECT to_regclass('public.inventory_balances') IS NOT NULL;";
        var tableExistsResult = await tableExistsCommand.ExecuteScalarAsync(cancellationToken);
        var tableExists = tableExistsResult is true;
        if (!tableExists)
        {
            return;
        }

        var firstPendingMigration = pendingMigrations.First();
        var efProductVersion = typeof(DbContext)
            .Assembly
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?.Split('+')[0] ?? "9.0.0";

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
}
