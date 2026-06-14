using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

/// <summary>
/// Seeds initial supply chain data for local development and E2E integration testing.
/// </summary>
public static class SupplyChainDataSeeder
{
    /// <summary>
    /// Seeds default suppliers, product SKU mappings, and material receipts if environment is Development.
    /// </summary>
    public static async Task SeedAsync(
        SupplyChainDbContext dbContext,
        string tenantCode,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        var hasSuppliers = await dbContext.Suppliers.IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (!hasSuppliers)
        {
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

        // Always ensure Metalúrgica Horizonte Ltda and its mappings exist for E2E tests
        var hasHorizonte = await dbContext.Suppliers.IgnoreQueryFilters().AnyAsync(s => s.FiscalId == FiscalId.From("64913382415254"), cancellationToken);
        if (!hasHorizonte)
        {
            logger.LogInformation("Seeding default Horizonte supplier for E2E tests...");
            var supplierHorizonte = Supplier.Create("64913382415254", "Metalúrgica Horizonte Ltda");
            dbContext.Suppliers.Add(supplierHorizonte);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var hasMappings = await dbContext.SupplierMaterialMappings.IgnoreQueryFilters().AnyAsync(m => m.SupplierFiscalId == FiscalId.From("64913382415254"), cancellationToken);
        if (!hasMappings)
        {
            logger.LogInformation("Seeding default Horizonte mappings for E2E tests...");
            var mappingHorizonteAco = SupplierMaterialMapping.Create(
                FiscalId.From("64913382415254"),
                "MAT-ACO-1020",
                MaterialCode.From("MAT-ACO-1020"),
                "KG",
                "KG",
                1.0m,
                EmailAddress.From("admin@railfactory.com.br"));

            var mappingHorizonteLub = SupplierMaterialMapping.Create(
                FiscalId.From("64913382415254"),
                "MAT-LUB-ISO68",
                MaterialCode.From("MAT-LUB-ISO68"),
                "L",
                "L",
                1.0m,
                EmailAddress.From("admin@railfactory.com.br"));

            dbContext.SupplierMaterialMappings.AddRange(mappingHorizonteAco, mappingHorizonteLub);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
