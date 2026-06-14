using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

/// <summary>
/// Seeds initial production data for local development.
/// </summary>
public static class ProductionDataSeeder
{
    /// <summary>
    /// Seeds default work centers, bill of materials (BOMs), and production orders if environment is Development.
    /// </summary>
    public static async Task SeedAsync(
        ProductionDbContext dbContext,
        string tenantCode,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) return;

        // 1. Create Work Centers
        var wcCor = await dbContext.WorkCenters.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Code == "WC-COR-01", cancellationToken);
        if (wcCor == null)
        {
            wcCor = WorkCenter.Create("WC-COR-01", "Linha de Corte e Guilhotina");
            dbContext.WorkCenters.Add(wcCor);
        }

        var wcSol = await dbContext.WorkCenters.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Code == "WC-SOL-02", cancellationToken);
        if (wcSol == null)
        {
            wcSol = WorkCenter.Create("WC-SOL-02", "Estação de Soldagem Robotizada");
            dbContext.WorkCenters.Add(wcSol);
        }

        var wcMon = await dbContext.WorkCenters.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Code == "WC-MON-03", cancellationToken);
        if (wcMon == null)
        {
            wcMon = WorkCenter.Create("WC-MON-03", "Linha de Montagem de Trilhos");
            dbContext.WorkCenters.Add(wcMon);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // 2. Create Bill of Materials (BOM)
        var productCode = MaterialCode.From("PRO-TR-100");
        var bom = await dbContext.Boms.Include(b => b.Items).IgnoreQueryFilters().FirstOrDefaultAsync(x => x.ProductCode == productCode && x.Version == 1, cancellationToken);
        if (bom == null)
        {
            bom = BillOfMaterials.Create("PRO-TR-100", 1, 1.0m);
            // Items: MAT-ACO-2MM (15.5 KG) and MAT-PAR-M8 (8.0 UN)
            bom.AddItem("MAT-ACO-2MM", 15.5m, "KG");
            bom.AddItem("MAT-PAR-M8", 8.0m, "UN");
            bom.Activate();
            dbContext.Boms.Add(bom);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Create Production Orders
        // OP-2026-0001 (Completed)
        var op1 = await dbContext.ProductionOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.OrderNumber == "OP-2026-0001", cancellationToken);
        if (op1 == null)
        {
            op1 = ProductionOrder.Create("OP-2026-0001", "PRO-TR-100", bom.Id, wcMon.Id, 10m);
            
            // Go through lifecycle to be completed
            op1.Release();
            op1.StartExecution();
            op1.Complete(inspectionPassed: true);

            dbContext.ProductionOrders.Add(op1);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Seed associated records for Completed OP
            // Inspection
            var inspection = QualityInspection.Create(op1.Id, InspectionResult.Passed, "Carlos Silva", "Trilho aprovado dimensionalmente e por ultrassom.");
            dbContext.QualityInspections.Add(inspection);

            // Consumptions
            var consAco = ConsumptionRecord.Create(op1.Id, "MAT-ACO-2MM", 155.0m, "KG");
            var consPar = ConsumptionRecord.Create(op1.Id, "MAT-PAR-M8", 80.0m, "UN");
            dbContext.ConsumptionRecords.AddRange(consAco, consPar);

            // Scrap
            var scrap = ScrapRecord.Create(op1.Id, "MAT-ACO-2MM", 5.0m, "KG", "Aparas de corte residual na guilhotina.");
            dbContext.ScrapRecords.Add(scrap);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // OP-2026-0002 (Released)
        var op2 = await dbContext.ProductionOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.OrderNumber == "OP-2026-0002", cancellationToken);
        if (op2 == null)
        {
            op2 = ProductionOrder.Create("OP-2026-0002", "PRO-TR-100", bom.Id, wcCor.Id, 25m);
            op2.Release();
            dbContext.ProductionOrders.Add(op2);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // OP-2026-0003 (Draft)
        var op3 = await dbContext.ProductionOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.OrderNumber == "OP-2026-0003", cancellationToken);
        if (op3 == null)
        {
            op3 = ProductionOrder.Create("OP-2026-0003", "PRO-TR-100", bom.Id, wcCor.Id, 50m);
            dbContext.ProductionOrders.Add(op3);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
