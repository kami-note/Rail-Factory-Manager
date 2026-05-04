using Microsoft.EntityFrameworkCore;
using RailFactory.Tenancy.Api.Domain;
using RailFactory.Tenancy.Api.Infrastructure.Persistence;
using System.Reflection;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class TenantCatalogSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<TenantCatalogSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedDevTenantAsync(dbContext, cancellationToken);

        logger.LogInformation("Tenant catalog schema initialized.");
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
        if (!tenantsTableExists)
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

    private static async Task SeedDevTenantAsync(TenancyDbContext dbContext, CancellationToken cancellationToken)
    {
        var tenant = Tenant.RegisterDevTenant();
        var existing = await dbContext.Tenants
            .SingleOrDefaultAsync(x => x.Code == tenant.Code, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            dbContext.Tenants.Add(new TenantRecord
            {
                Code = tenant.Code,
                DisplayName = tenant.DisplayName,
                Locale = tenant.Locale,
                TimeZone = tenant.TimeZone,
                Status = tenant.Status.ToString(),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.DisplayName = tenant.DisplayName;
            existing.Locale = tenant.Locale;
            existing.TimeZone = tenant.TimeZone;
            existing.Status = tenant.Status.ToString();
            existing.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
