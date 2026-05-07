using Microsoft.EntityFrameworkCore;
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
        return dbContext.Materials
            .FirstOrDefaultAsync(x => x.MaterialCode == materialCode, cancellationToken);
    }

    public Task<Dictionary<string, Material>> GetByCodesAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken)
    {
        return dbContext.Materials
            .Where(x => materialCodes.Contains(x.MaterialCode))
            .ToDictionaryAsync(x => x.MaterialCode, cancellationToken);
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
