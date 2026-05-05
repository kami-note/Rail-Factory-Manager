using Microsoft.EntityFrameworkCore;
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

    public Task<List<InventoryBalance>> ListPendingBalancesAsync(CancellationToken cancellationToken)
        => dbContext.Balances
            .AsNoTracking()
            .Where(x => x.Status == InventoryBalanceStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
