using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Iam.Api.Infrastructure.Auth.Persistence;
using System.Reflection;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

public sealed class IamLocalUsersSchemaInitializer(
    IServiceProvider serviceProvider,
    ILogger<IamLocalUsersSchemaInitializer> logger) : BackgroundService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);
    private readonly HashSet<string> _migratedTenants = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IAM schema initializer started.");
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

        logger.LogInformation("Migrating IAM databases for {Count} new tenant(s)...", pending.Count);
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

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<IamAuthDbContext>();
            await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
            await SeedTenantRolesAsync(dbContext, tenant.Code, cancellationToken);

            _migratedTenants.Add(tenant.Code);
            logger.LogInformation("IAM database for tenant '{TenantCode}' migrated.", tenant.Code);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to migrate IAM database for tenant '{TenantCode}'. Will retry.", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        IamAuthDbContext dbContext,
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
        tableExistsCommand.CommandText = "SELECT to_regclass('public.iam_local_users') IS NOT NULL;";
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

    private async Task SeedTenantRolesAsync(IamAuthDbContext dbContext, string tenantCode, CancellationToken cancellationToken)
    {
        var hasRoles = await dbContext.Roles.AnyAsync(cancellationToken);
        if (hasRoles) return;

        logger.LogInformation("Seeding default roles for tenant '{TenantCode}'...", tenantCode);

        dbContext.Roles.AddRange(
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Administrador do Sistema",
                Description = "Acesso total a todos os módulos e gestão de usuários.",
                Permissions = SystemPermissions.All().ToList(),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Operador de Logística",
                Description = "Gerencia expedição, despachos, transportadoras, recebimentos e estoque.",
                Permissions =
                [
                    SystemPermissions.Inventory.Read, SystemPermissions.Inventory.Write,
                    SystemPermissions.SupplyChain.Read, SystemPermissions.SupplyChain.Write,
                    SystemPermissions.Logistics.Read, SystemPermissions.Logistics.Write,
                    SystemPermissions.Hr.Read,
                    SystemPermissions.Fleet.Read, SystemPermissions.Fleet.Write,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Consulta (Apenas Leitura)",
                Description = "Acesso de visualização para todos os módulos.",
                Permissions =
                [
                    SystemPermissions.Inventory.Read, SystemPermissions.SupplyChain.Read,
                    SystemPermissions.Production.Read, SystemPermissions.Iam.Read,
                    SystemPermissions.Hr.Read, SystemPermissions.Fleet.Read, SystemPermissions.Logistics.Read,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Supervisor de RH e Frota",
                Description = "Gerencia cadastro de pessoas, apontamentos de horas, veículos e alocações de motoristas.",
                Permissions =
                [
                    SystemPermissions.Hr.Read, SystemPermissions.Hr.Write,
                    SystemPermissions.Fleet.Read, SystemPermissions.Fleet.Write,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Operador de Produção",
                Description = "Gerencia ordens de produção, BOMs, centros de trabalho e consulta estoque.",
                Permissions =
                [
                    SystemPermissions.Production.Read, SystemPermissions.Production.Write,
                    SystemPermissions.Inventory.Read, SystemPermissions.Hr.Read,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new IamTenantRoleRecord
            {
                Id = Guid.NewGuid(),
                TenantCode = tenantCode,
                Name = "Responsável Fiscal",
                Description = "Gerencia documentos fiscais NF-e de saída: emissão, reemissão e monitor fiscal.",
                Permissions =
                [
                    SystemPermissions.Logistics.Read, SystemPermissions.Logistics.Fiscal,
                    SystemPermissions.Inventory.Read, SystemPermissions.SupplyChain.Read,
                ],
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
