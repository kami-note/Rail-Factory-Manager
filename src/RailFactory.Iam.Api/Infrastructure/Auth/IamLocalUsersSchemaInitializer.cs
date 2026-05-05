using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;
using System.Reflection;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

public sealed class IamLocalUsersSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<IamLocalUsersSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await EnsureDefaultTenantContextAsync(scope.ServiceProvider, cancellationToken);
        var dbContext = scope.ServiceProvider.GetRequiredService<IamAuthDbContext>();

        await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("IAM local users schema initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureDefaultTenantContextAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var options = services.GetRequiredService<IOptions<TenantRoutingOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.DefaultTenantCode))
        {
            throw new InvalidOperationException("TenantRouting:DefaultTenantCode is required for IAM schema initialization.");
        }

        var catalogClient = services.GetRequiredService<ITenantCatalogClient>();
        var tenant = await catalogClient.ResolveAsync(options.DefaultTenantCode, cancellationToken);
        if (!tenant.Found || !tenant.IsActive)
        {
            throw new InvalidOperationException($"Default tenant '{options.DefaultTenantCode}' was not found or is inactive.");
        }

        var tenantContextAccessor = services.GetRequiredService<ITenantContextAccessor>();
        tenantContextAccessor.Current = new TenantContext(tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);
    }

    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        IamAuthDbContext dbContext,
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
        tableExistsCommand.CommandText = "SELECT to_regclass('public.iam_local_users') IS NOT NULL;";
        var tableExistsResult = await tableExistsCommand.ExecuteScalarAsync(cancellationToken);
        var iamLocalUsersTableExists = tableExistsResult is true;
        if (!iamLocalUsersTableExists)
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
}
