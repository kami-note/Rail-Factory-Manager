using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Infrastructure.Persistence;

public sealed class PostgresBomRepository(ProductionDbContext context) : IBomRepository
{
    public async Task AddAsync(BillOfMaterials bom, CancellationToken cancellationToken)
        => await context.Boms.AddAsync(bom, cancellationToken);

    public Task<BillOfMaterials?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.Boms.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<BillOfMaterials>> ListByProductCodeAsync(string productCode, CancellationToken cancellationToken)
    {
        var code = MaterialCode.From(productCode).Value;
        return context.Boms
            .Include(x => x.Items)
            .Where(x => x.ProductCode == MaterialCode.From(code))
            .OrderByDescending(x => x.Version)
            .ToListAsync(cancellationToken);
    }

    public Task<List<BillOfMaterials>> ListAllAsync(CancellationToken cancellationToken)
        => context.Boms
            .Include(x => x.Items)
            .OrderBy(x => x.ProductCode)
            .ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

    public Task<BillOfMaterials?> GetActiveByProductCodeAsync(string productCode, CancellationToken cancellationToken)
    {
        var code = MaterialCode.From(productCode).Value;
        return context.Boms
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.ProductCode == MaterialCode.From(code) && x.Status == BomStatus.Active,
                cancellationToken);
    }

    public async Task<int> GetLatestVersionNumberAsync(string productCode, CancellationToken cancellationToken)
    {
        var code = MaterialCode.From(productCode).Value;
        return await context.Boms
            .Where(x => x.ProductCode == MaterialCode.From(code))
            .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public async Task AddItemDirectAsync(Guid bomId, BomItem item, DateTimeOffset bomUpdatedAt, CancellationToken cancellationToken)
    {
        // Bypass EF Core change tracking entirely to avoid Npgsql ValueGeneratedOnAdd conflicts.
        // EF Core 10 + Npgsql 10 treats new BomItem entities as Unchanged (non-zero Guid PK with
        // ValueGeneratedOnAdd convention), causing SaveChanges to generate an UPDATE on a non-existent
        // row → DbUpdateConcurrencyException. Raw SQL is the reliable escape hatch.
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO bom_items ("Id", "BomId", "MaterialCode", "Quantity", "UnitOfMeasure")
            VALUES ({item.Id}, {bomId}, {item.MaterialCode.Value}, {item.Quantity}, {item.UnitOfMeasure})
            """,
            cancellationToken);

        await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE boms SET "UpdatedAt" = {bomUpdatedAt} WHERE "Id" = {bomId}
            """,
            cancellationToken);
    }
}
