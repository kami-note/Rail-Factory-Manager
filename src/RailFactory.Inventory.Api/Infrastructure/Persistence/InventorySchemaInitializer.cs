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
        if (!environment.IsDevelopment()) return;

        // 1. Ensure Almoxarifado Central (StockLocation) exists
        var stockLocation = await dbContext.StockLocations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Code == "ALM-CENTRAL", cancellationToken);
        if (stockLocation == null)
        {
            stockLocation = StockLocation.Create("ALM-CENTRAL", "Almoxarifado Central");
            dbContext.StockLocations.Add(stockLocation);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Ensure Materials exist
        var matAcoCode = MaterialCode.From("MAT-ACO-2MM");
        var hasMatAco = await dbContext.Materials.IgnoreQueryFilters().AnyAsync(m => m.MaterialCode == matAcoCode, cancellationToken);
        if (!hasMatAco)
        {
            var matAco = Material.Create(
                "MAT-ACO-2MM",
                "Chapa de Aço Galvanizado 2mm",
                "Chapa de aço plano galvanizado com espessura de 2mm utilizada para produção ferroviária.",
                MaterialCategory.RawMaterial,
                ProcurementType.Buy,
                EmailAddress.From("admin@railfactory.com.br"),
                "KG",
                MaterialStatus.Verified,
                ncm: "72085100"
            );
            dbContext.Materials.Add(matAco);
        }

        var matParCode = MaterialCode.From("MAT-PAR-M8");
        var hasMatPar = await dbContext.Materials.IgnoreQueryFilters().AnyAsync(m => m.MaterialCode == matParCode, cancellationToken);
        if (!hasMatPar)
        {
            var matPar = Material.Create(
                "MAT-PAR-M8",
                "Parafuso Sextavado M8",
                "Parafuso de cabeça sextavada bitola M8 para fixação mecânica.",
                MaterialCategory.RawMaterial,
                ProcurementType.Buy,
                EmailAddress.From("admin@railfactory.com.br"),
                "UN",
                MaterialStatus.Verified,
                ncm: "73181500"
            );
            dbContext.Materials.Add(matPar);
        }

        var proTrCode = MaterialCode.From("PRO-TR-100");
        var hasProTr = await dbContext.Materials.IgnoreQueryFilters().AnyAsync(m => m.MaterialCode == proTrCode, cancellationToken);
        if (!hasProTr)
        {
            var proTr = Material.Create(
                "PRO-TR-100",
                "Trilho Ferroviário TR-100",
                "Trilho ferroviário padrão TR-100 de alta resistência para vias permanentes.",
                MaterialCategory.FinishedGood,
                ProcurementType.Make,
                EmailAddress.From("admin@railfactory.com.br"),
                "UN",
                MaterialStatus.Verified,
                ncm: "73021010"
            );
            dbContext.Materials.Add(proTr);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. Ensure Inventory Balances exist (idempotent via SourceReference)
        // For MAT-ACO-2MM (1200 KG)
        var hasBalanceAco = await dbContext.Balances.IgnoreQueryFilters().AnyAsync(b => b.SourceReference == "seed-init:MAT-ACO-2MM", cancellationToken);
        if (!hasBalanceAco)
        {
            var balanceAco = InventoryBalance.CreatePending(
                "MAT-ACO-2MM",
                "KG",
                1200.0m,
                stockLocation.Id,
                "seed-init:MAT-ACO-2MM",
                "L-ACO-2026-01",
                DateTimeOffset.UtcNow.AddYears(1),
                InventorySourceType.Purchase,
                null
            );
            balanceAco.Confirm(1200.0m, "L-ACO-2026-01", DateTimeOffset.UtcNow.AddYears(1), isApproved: true);
            dbContext.Balances.Add(balanceAco);
        }

        // For MAT-PAR-M8 (5000 UN)
        var hasBalancePar = await dbContext.Balances.IgnoreQueryFilters().AnyAsync(b => b.SourceReference == "seed-init:MAT-PAR-M8", cancellationToken);
        if (!hasBalancePar)
        {
            var balancePar = InventoryBalance.CreatePending(
                "MAT-PAR-M8",
                "UN",
                5000.0m,
                stockLocation.Id,
                "seed-init:MAT-PAR-M8",
                "L-PAR-2026-02",
                DateTimeOffset.UtcNow.AddYears(1),
                InventorySourceType.Purchase,
                null
            );
            balancePar.Confirm(5000.0m, "L-PAR-2026-02", DateTimeOffset.UtcNow.AddYears(1), isApproved: true);
            dbContext.Balances.Add(balancePar);
        }

        // For PRO-TR-100 (25 UN)
        var hasBalanceTr = await dbContext.Balances.IgnoreQueryFilters().AnyAsync(b => b.SourceReference == "seed-init:PRO-TR-100", cancellationToken);
        if (!hasBalanceTr)
        {
            var balanceTr = InventoryBalance.CreatePending(
                "PRO-TR-100",
                "UN",
                25.0m,
                stockLocation.Id,
                "seed-init:PRO-TR-100",
                "L-TR-2026-01",
                DateTimeOffset.UtcNow.AddYears(2),
                InventorySourceType.Production,
                null
            );
            balanceTr.Confirm(25.0m, "L-TR-2026-01", DateTimeOffset.UtcNow.AddYears(2), isApproved: true);
            dbContext.Balances.Add(balanceTr);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
