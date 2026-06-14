using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

/// <summary>
/// Seeds initial inventory data for local development.
/// </summary>
public static class InventoryDataSeeder
{
    /// <summary>
    /// Seeds default stock locations, materials, and inventory balances if environment is Development.
    /// </summary>
    public static async Task SeedAsync(
        InventoryDbContext dbContext,
        string tenantCode,
        IHostEnvironment environment,
        ILogger logger,
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

        var matAco1020Code = MaterialCode.From("MAT-ACO-1020");
        var hasMatAco1020 = await dbContext.Materials.IgnoreQueryFilters().AnyAsync(m => m.MaterialCode == matAco1020Code, cancellationToken);
        if (!hasMatAco1020)
        {
            var matAco1020 = Material.Create(
                "MAT-ACO-1020",
                "Chapa de Aço Carbono SAE 1020",
                "Chapa de Aço Carbono SAE 1020 - 1/4 pol",
                MaterialCategory.RawMaterial,
                ProcurementType.Buy,
                EmailAddress.From("admin@railfactory.com.br"),
                "KG",
                MaterialStatus.Verified,
                ncm: "72085100"
            );
            dbContext.Materials.Add(matAco1020);
        }

        var matLubCode = MaterialCode.From("MAT-LUB-ISO68");
        var hasMatLub = await dbContext.Materials.IgnoreQueryFilters().AnyAsync(m => m.MaterialCode == matLubCode, cancellationToken);
        if (!hasMatLub)
        {
            var matLub = Material.Create(
                "MAT-LUB-ISO68",
                "Óleo Lubrificante Industrial ISO VG 68",
                "Óleo Lubrificante Industrial ISO VG 68",
                MaterialCategory.RawMaterial,
                ProcurementType.Buy,
                EmailAddress.From("admin@railfactory.com.br"),
                "L",
                MaterialStatus.Verified,
                ncm: "27101932"
            );
            dbContext.Materials.Add(matLub);
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
