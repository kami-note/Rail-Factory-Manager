using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

public sealed class PostgresInventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    public Task<StockLocation?> FindDefaultLocationAsync(CancellationToken cancellationToken)
        => dbContext.StockLocations.FirstOrDefaultAsync(x => x.Code == "PENDING", cancellationToken);

    public async Task EnsureDefaultLocationAsync(CancellationToken cancellationToken)
    {
        var existing = await FindDefaultLocationAsync(cancellationToken);
        if (existing is not null)
        {
            return;
        }

        await dbContext.StockLocations.AddAsync(StockLocation.Create("PENDING", "Pending Receipt Area"), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IntegrationMessageProcessedAsync(Guid eventId, CancellationToken cancellationToken)
        => dbContext.ProcessedIntegrationMessages.AnyAsync(x => x.EventId == eventId, cancellationToken);

    public Task AddIntegrationMessageAsync(InventoryIntegrationMessage message, CancellationToken cancellationToken)
        => dbContext.ProcessedIntegrationMessages.AddAsync(message, cancellationToken).AsTask();

    public Task AddBalanceAsync(InventoryBalance balance, CancellationToken cancellationToken)
        => dbContext.Balances.AddAsync(balance, cancellationToken).AsTask();

    public Task AddLedgerEntryAsync(InventoryLedgerEntry entry, CancellationToken cancellationToken)
        => dbContext.LedgerEntries.AddAsync(entry, cancellationToken).AsTask();

    public Task<InventoryBalance?> GetBalanceByIdAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Balances.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<InventoryBalance?> GetBalanceBySourceReferenceAsync(string sourceReference, CancellationToken cancellationToken)
        => dbContext.Balances.FirstOrDefaultAsync(x => x.SourceReference == sourceReference, cancellationToken);

    public Task<InventoryBalance?> GetLatestBalanceByMaterialCodeAsync(string materialCode, CancellationToken cancellationToken)
    {
        var code = MaterialCode.From(materialCode);
        return dbContext.Balances
            .Where(x => x.MaterialCode == code)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<InventoryLedgerEntry>> GetLedgerEntriesByBalanceIdAsync(Guid balanceId, CancellationToken cancellationToken)
        => dbContext.LedgerEntries
            .Where(x => x.BalanceId == balanceId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<InventoryBalance>> ListBalancesAsync(InventoryBalanceStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Balances.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query.OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
