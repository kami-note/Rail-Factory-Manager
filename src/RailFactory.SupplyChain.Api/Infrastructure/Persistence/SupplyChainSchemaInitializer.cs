using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyChainSchemaInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<SupplyChainSchemaInitializer> logger) : BackgroundService
{
    private static readonly SemaphoreSlim MigrationSemaphore = new(5);
    private readonly HashSet<string> _migratedTenants = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SupplyChain schema initializer started.");
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

        logger.LogInformation("Migrating SupplyChain databases for {Count} new tenant(s)...", pending.Count);
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

            var dbContext = tenantScope.ServiceProvider.GetRequiredService<SupplyChainDbContext>();
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                await AlignLegacySchemaWithMigrationHistoryAsync(dbContext, cancellationToken);
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
            await SeedSupplyChainDataAsync(dbContext, tenant.Code, cancellationToken);
            await TenantServiceReadiness.MarkReadyAsync(dbContext.Database.GetDbConnection(), cancellationToken);

            _migratedTenants.Add(tenant.Code);
            logger.LogInformation("SupplyChain database for tenant '{TenantCode}' migrated.", tenant.Code);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Failed to migrate SupplyChain database for tenant '{TenantCode}'. Will retry.", tenant.Code);
        }
        finally
        {
            MigrationSemaphore.Release();
        }
    }

    private static async Task AlignLegacySchemaWithMigrationHistoryAsync(
        SupplyChainDbContext dbContext,
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
        tableExistsCommand.CommandText = "SELECT to_regclass('public.suppliers') IS NOT NULL;";
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

    private async Task SeedSupplyChainDataAsync(
        SupplyChainDbContext dbContext,
        string tenantCode,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        var hasSuppliers = await dbContext.Suppliers.IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (hasSuppliers) return;

        logger.LogInformation("Seeding default SupplyChain data for tenant '{TenantCode}'...", tenantCode);

        // 1. Create Suppliers
        var supplierAcme = Supplier.Create("12345678000195", "ACME Metalúrgica Ltda");
        var supplierGlobal = Supplier.Create("98765432000198", "Global Parafusos S.A.");
        dbContext.Suppliers.AddRange(supplierAcme, supplierGlobal);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 2. Create Mappings (Supplier SKU to Internal SKU)
        var mappingAcme = SupplierMaterialMapping.Create(
            supplierAcme.FiscalId,
            "SUP-ACO-2MM",             // Supplier Product Code
            MaterialCode.From("MAT-ACO-2MM"),  // Internal Material Code
            "KG",                      // Internal UoM
            "KG",                      // Supplier Unit
            1.0m,                      // Conversion Factor
            EmailAddress.From("admin@railfactory.com.br"));

        var mappingGlobal = SupplierMaterialMapping.Create(
            supplierGlobal.FiscalId,
            "SUP-PAR-M8",              // Supplier Product Code
            MaterialCode.From("MAT-PAR-M8"),  // Internal Material Code
            "UN",                      // Internal UoM
            "UN",                      // Supplier Unit
            1.0m,                      // Conversion Factor
            EmailAddress.From("admin@railfactory.com.br"));

        dbContext.SupplierMaterialMappings.AddRange(mappingAcme, mappingGlobal);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. Create Receipts (Invoices)
        // Approved Receipt
        var receiptApproved = MaterialReceipt.Create(
            "NFE-00012345",
            supplierAcme.Id,
            "00012345",
            "35260612345678000195550010000123451000123456",
            4250.00m,
            "<nfeProc>ACME Invoice</nfeProc>",
            new DateOnly(2026, 6, 10),
            FiscalEnvironment.Production);
        
        receiptApproved.AddItem(
            "MAT-ACO-2MM",             // Material Code
            "KG",                      // Unit of measure
            500.0m,                    // Expected Qty
            8.50m,                     // Unit price
            "CHAPA DE ACO GALVANIZADO 2MM", // Description
            "72085100",                // NCM
            "5102",                    // CFOP
            "7891234567890"            // EAN
        );

        // Close conference for this receipt to approve it (so it has balance in stock)
        receiptApproved.StartConference();
        var approvedItemsResults = receiptApproved.Items.Select(item =>
            new CountedItemResult(item.Id, 500.0m, "L-ACO-2026-01", DateTimeOffset.UtcNow.AddYears(1))
        ).ToList();
        receiptApproved.CloseConference(approvedItemsResults);

        // InConference Receipt
        var receiptInConference = MaterialReceipt.Create(
            "NFE-00012346",
            supplierGlobal.Id,
            "00012346",
            "35260698765432000198550010000123461000123467",
            300.00m,
            "<nfeProc>Global Invoice</nfeProc>",
            new DateOnly(2026, 6, 10),
            FiscalEnvironment.Production);

        receiptInConference.AddItem(
            "MAT-PAR-M8",              // Material Code
            "UN",                      // Unit of measure
            2000.0m,                   // Expected Qty
            0.15m,                     // Unit price
            "PARAFUSO SEXTAVADO M8",   // Description
            "73181500",                // NCM
            "5102",                    // CFOP
            "7899876543210"            // EAN
        );

        receiptInConference.StartConference();

        dbContext.Receipts.AddRange(receiptApproved, receiptInConference);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
