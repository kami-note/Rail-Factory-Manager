using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using RailFactory.Tenancy.Api.Application;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class TenantCatalogSchemaInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<TenantCatalogSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        logger.LogInformation("Initializing tenant catalog schema...");
        if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        logger.LogInformation("Tenant catalog schema initialized successfully.");

        if (environment.IsDevelopment())
        {
            await SeedDefaultTenantsAsync(scope, dbContext, cancellationToken);
        }
    }

    private async Task SeedDefaultTenantsAsync(
        AsyncServiceScope scope,
        TenancyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking default tenants seed...");
        var registerTenant = scope.ServiceProvider.GetRequiredService<RegisterTenant>();

        await SeedTenantIfNotExistAsync(registerTenant, dbContext, "dev", "Desenvolvimento Local", cancellationToken);
        await SeedTenantIfNotExistAsync(registerTenant, dbContext, "acme", "Acme Corporation", cancellationToken);
    }

    private async Task SeedTenantIfNotExistAsync(
        RegisterTenant registerTenant,
        TenancyDbContext dbContext,
        string code,
        string displayName,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Tenants.AnyAsync(t => t.Code == code, cancellationToken);
        if (!exists)
        {
            logger.LogInformation("Seeding tenant '{TenantCode}'...", code);
            try
            {
                var result = await registerTenant.ExecuteAsync(
                    new RegisterTenantInput(code, displayName), cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogInformation("Tenant '{TenantCode}' seeded successfully.", code);
                }
                else
                {
                    logger.LogError("Failed to seed tenant '{TenantCode}': {Error}", code, result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed tenant '{TenantCode}' due to database exception.", code);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        TenancyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL") return;

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (!pendingMigrations.Any()) return;

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var tenantsTableExists = await ColumnExistsAsync(connection, "tenants", null, cancellationToken);
        if (!tenantsTableExists) return;

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

    private static readonly HashSet<string> AllowedTables = ["tenants", "tenant_integrations"];
    private static readonly HashSet<string> AllowedColumns = ["connection_strings"];

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
}
