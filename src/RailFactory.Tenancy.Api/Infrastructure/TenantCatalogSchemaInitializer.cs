using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;
using System.Reflection;

namespace RailFactory.Tenancy.Api.Infrastructure;

/// <summary>
/// Initializes the global Tenant Catalog database schema and performs initial seeding.
/// </summary>
/// <remarks>
/// This initializer manages the shared catalog of tenants. It ensures the migration history 
/// is aligned with legacy schemas if necessary and applies the latest migrations.
/// It also seeds the system with initial tenants (e.g., 'dev' and 'acme') to ensure 
/// the platform is ready for operation immediately after deployment.
/// </remarks>
public sealed class TenantCatalogSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<TenantCatalogSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        logger.LogInformation("Initializing tenant catalog schema...");
        await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        
        await SeedDevTenantAsync(dbContext, cancellationToken);
        await SeedAcmeTenantAsync(dbContext, cancellationToken);

        logger.LogInformation("Tenant catalog schema initialized successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        TenancyDbContext dbContext,
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
        tableExistsCommand.CommandText = "SELECT to_regclass('public.tenants') IS NOT NULL;";
        var tableExistsResult = await tableExistsCommand.ExecuteScalarAsync(cancellationToken);
        var tenantsTableExists = tableExistsResult is true;
        if (! tenantsTableExists)
        {
            return;
        }

        var firstPendingMigration = pendingMigrations.First();
        var efProductVersion = typeof(DbContext)
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
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

    private async Task SeedDevTenantAsync(TenancyDbContext dbContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding 'dev' tenant...");
        var tenant = Tenant.RegisterDevTenant();
        tenant.SetConnectionString("iamdb", "tenant-dev-iamdb");
        tenant.SetConnectionString("supplychaindb", "tenant-dev-supplychaindb");
        tenant.SetConnectionString("inventorydb", "tenant-dev-inventorydb");
        tenant.SetConnectionString("productiondb", "tenant-dev-productiondb");

        await UpsertTenantAsync(dbContext, tenant, cancellationToken);
    }

    private async Task SeedAcmeTenantAsync(TenancyDbContext dbContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding 'acme' tenant...");
        var tenant = Tenant.Restore(
            "acme",
            "Acme Corporation",
            "en-US",
            "UTC",
            TenantStatus.Active);

        tenant.SetConnectionString("iamdb", "tenant-acme-iamdb");
        tenant.SetConnectionString("supplychaindb", "tenant-acme-supplychaindb");
        tenant.SetConnectionString("inventorydb", "tenant-acme-inventorydb");
        tenant.SetConnectionString("productiondb", "tenant-acme-productiondb");

        await UpsertTenantAsync(dbContext, tenant, cancellationToken);
    }

    private async Task UpsertTenantAsync(TenancyDbContext dbContext, Tenant tenant, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Tenants
            .SingleOrDefaultAsync(x => x.Code == tenant.Code, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            logger.LogInformation("Adding new tenant: {TenantCode}", tenant.Code);
            dbContext.Tenants.Add(new TenantRecord
            {
                Code = tenant.Code,
                DisplayName = tenant.DisplayName,
                Locale = tenant.Locale,
                TimeZone = tenant.TimeZone,
                Status = tenant.Status.ToString(),
                ConnectionStrings = tenant.ConnectionStrings.ToDictionary(x => x.Key, x => x.Value),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            logger.LogInformation("Updating existing tenant: {TenantCode}", tenant.Code);
            existing.DisplayName = tenant.DisplayName;
            existing.Locale = tenant.Locale;
            existing.TimeZone = tenant.TimeZone;
            existing.Status = tenant.Status.ToString();
            existing.ConnectionStrings = tenant.ConnectionStrings.ToDictionary(x => x.Key, x => x.Value);
            existing.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
