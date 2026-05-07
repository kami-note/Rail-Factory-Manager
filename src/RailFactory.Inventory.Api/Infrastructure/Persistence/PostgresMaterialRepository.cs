using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL implementation of the Material repository.
/// </summary>
public sealed class PostgresMaterialRepository(InventoryDbContext dbContext) : IMaterialRepository
{
    public Task<Material?> GetByCodeAsync(string materialCode, CancellationToken cancellationToken)
    {
        var code = MaterialCode.From(materialCode);
        return dbContext.Materials
            .FirstOrDefaultAsync(x => x.MaterialCode == code, cancellationToken);
    }

    public Task<Dictionary<string, Material>> GetByCodesAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken)
    {
        var codes = materialCodes.Select(MaterialCode.From).ToList();
        return dbContext.Materials
            .Where(x => codes.Contains(x.MaterialCode))
            .ToDictionaryAsync(x => x.MaterialCode.Value, cancellationToken);
    }

    public async Task AddAsync(Material material, CancellationToken cancellationToken)
    {
        await dbContext.Materials.AddAsync(material, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
