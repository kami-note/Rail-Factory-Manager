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

    /// <summary>
    /// Inspects the actual database schema and marks already-applied migrations as done
    /// so EF Core does not try to re-run them. Handles legacy databases that were created
    /// before the EF migration system was introduced.
    /// </summary>
    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        TenancyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (!pendingMigrations.Any())
            return;

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        // Check what already exists in the database
        var tenantsTableExists = await ColumnExistsAsync(connection, "tenants", null, cancellationToken);
        if (!tenantsTableExists)
            return; // truly fresh database — let MigrateAsync create everything

        var connectionStringsColumnExists = await ColumnExistsAsync(connection, "tenants", "connection_strings", cancellationToken);
        var tenantIntegrationsTableExists = await ColumnExistsAsync(connection, "tenant_integrations", null, cancellationToken);

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

        // Mark each legacy migration as applied only when we confirm its schema change already exists
        foreach (var migration in pendingMigrations)
        {
            var alreadyApplied = migration switch
            {
                var m when m.Contains("InitialTenantCatalog")       => tenantsTableExists,
                var m when m.Contains("AddTenantConnectionStrings") => connectionStringsColumnExists,
                var m when m.Contains("AddTenantIntegrations")      => tenantIntegrationsTableExists,
                _ => false
            };

            if (alreadyApplied)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ({migration}, {efProductVersion})
                    ON CONFLICT ("MigrationId") DO NOTHING;
                    """);
            }
        }
    }

    private static readonly HashSet<string> AllowedTables =
        ["tenants", "tenant_integrations"];
    private static readonly HashSet<string> AllowedColumns =
        ["connection_strings"];

    private static async Task<bool> ColumnExistsAsync(
        System.Data.Common.DbConnection connection,
        string table,
        string? column,
        CancellationToken cancellationToken)
    {
        if (!AllowedTables.Contains(table))
            throw new ArgumentException($"Table '{table}' is not in the schema-check allowlist.", nameof(table));
        if (column is not null && !AllowedColumns.Contains(column))
            throw new ArgumentException($"Column '{column}' is not in the schema-check allowlist.", nameof(column));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = column is null
            ? $"SELECT to_regclass('public.{table}') IS NOT NULL"
            : $"SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='{table}' AND column_name='{column}')";
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private async Task SeedDevTenantAsync(TenancyDbContext dbContext, CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding 'dev' tenant...");
        var tenant = Tenant.RegisterDevTenant();
        tenant.SetConnectionString("iamdb", "ConnectionStrings:tenant-dev-iamdb");
        tenant.SetConnectionString("supplychaindb", "ConnectionStrings:tenant-dev-supplychaindb");
        tenant.SetConnectionString("inventorydb", "ConnectionStrings:tenant-dev-inventorydb");
        tenant.SetConnectionString("productiondb", "ConnectionStrings:tenant-dev-productiondb");
        tenant.SetConnectionString("hrdb", "ConnectionStrings:tenant-dev-hrdb");
        tenant.SetConnectionString("fleetdb", "ConnectionStrings:tenant-dev-fleetdb");
        tenant.SetConnectionString("logisticsdb", "ConnectionStrings:tenant-dev-logisticsdb");

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

        tenant.SetConnectionString("iamdb", "ConnectionStrings:tenant-acme-iamdb");
        tenant.SetConnectionString("supplychaindb", "ConnectionStrings:tenant-acme-supplychaindb");
        tenant.SetConnectionString("inventorydb", "ConnectionStrings:tenant-acme-inventorydb");
        tenant.SetConnectionString("productiondb", "ConnectionStrings:tenant-acme-productiondb");
        tenant.SetConnectionString("hrdb", "ConnectionStrings:tenant-acme-hrdb");
        tenant.SetConnectionString("fleetdb", "ConnectionStrings:tenant-acme-fleetdb");
        tenant.SetConnectionString("logisticsdb", "ConnectionStrings:tenant-acme-logisticsdb");

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
