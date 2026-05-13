using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;
using System.Reflection;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

/// <summary>
/// Initializes the IAM database schema for all active tenants.
/// </summary>
/// <remarks>
/// This initializer iterates over all tenants in the catalog and applies EF Core migrations.
/// It uses a <see cref="SemaphoreSlim"/> to limit concurrent database migrations,
/// ensuring stability and preventing connection pool exhaustion during startup.
/// Resilience is handled by the underlying <see cref="ITenantCatalogClient"/>'s resilience policies.
/// </remarks>
public sealed class IamLocalUsersSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<IamLocalUsersSchemaInitializer> logger) : IHostedService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting IAM multi-tenant schema initialization...");

        await using var scope = serviceProvider.CreateAsyncScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
        var tenantContextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();

        var allTenants = await catalogClient.ListAllAsync(cancellationToken);
        var activeTenants = allTenants.Where(t => t.Found && t.IsActive).ToList();

        if (!activeTenants.Any())
        {
            logger.LogWarning("No active tenants found in catalog for IAM migration.");
            return;
        }

        var migrationTasks = activeTenants.Select(tenant => MigrateTenantAsync(tenant, tenantContextAccessor, cancellationToken));
        await Task.WhenAll(migrationTasks);

        logger.LogInformation("IAM multi-tenant schema initialization completed.");
    }

    private async Task MigrateTenantAsync(
        TenantResolutionResult tenant,
        ITenantContextAccessor tenantContextAccessor,
        CancellationToken cancellationToken)
    {
        await MigrationSemaphore.WaitAsync(cancellationToken);
        try
        {
            logger.LogInformation("Migrating IAM database for tenant: {TenantCode}", tenant.Code);

            // Set context for this tenant so DbContext can resolve the correct connection string
            // We use a local-only assignment here, but because we create a NEW scope below, 
            // the DbContext in that scope will read from this accessor if it's Scoped.
            // NOTE: In parallel execution, we must ensure the ITenantContextAccessor is handled correctly.
            // Since ITenantContextAccessor is Scoped, we need to set it WITHIN the scope we are using.

            using var tenantScope = serviceProvider.CreateScope();
            var scopedContextAccessor = tenantScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            scopedContextAccessor.Current = new TenantContext(
                tenant.Code,
                tenant.Locale,
                tenant.TimeZone,
                tenant.ConnectionStrings);

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<IamAuthDbContext>();

            await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);

            if (tenant.Code == "dev")
            {
                await SeedTenantRolesAsync(dbContext, tenant.Code, cancellationToken);
            }

            logger.LogInformation("IAM database for tenant {TenantCode} initialized successfully.", tenant.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to migrate IAM database for tenant: {TenantCode}", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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

    private async Task SeedTenantRolesAsync(IamAuthDbContext dbContext, string tenantCode, CancellationToken cancellationToken)
    {
        var hasRoles = await dbContext.Roles.AnyAsync(cancellationToken);
        if (hasRoles) return;

        logger.LogInformation("Seeding default roles for 'dev' tenant...");

        var adminRole = new IamTenantRoleRecord
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            TenantCode = tenantCode,
            Name = "Administrador do Sistema",
            Description = "Acesso total a todos os módulos e gestão de usuários.",
            Permissions = SystemPermissions.All().ToList(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var operatorRole = new IamTenantRoleRecord
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            TenantCode = tenantCode,
            Name = "Operador de Logística",
            Description = "Acesso para leitura e escrita em estoque e recebimentos.",
            Permissions = 
            [
                SystemPermissions.Inventory.Read, 
                SystemPermissions.Inventory.Write,
                SystemPermissions.SupplyChain.Read,
                SystemPermissions.SupplyChain.Write
            ],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var viewerRole = new IamTenantRoleRecord
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
            TenantCode = tenantCode,
            Name = "Consulta (Apenas Leitura)",
            Description = "Acesso de visualização para dashboards e relatórios.",
            Permissions = 
            [
                SystemPermissions.Inventory.Read, 
                SystemPermissions.SupplyChain.Read,
                SystemPermissions.Production.Read,
                SystemPermissions.Iam.Read
            ],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Roles.AddRange(adminRole, operatorRole, viewerRole);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
