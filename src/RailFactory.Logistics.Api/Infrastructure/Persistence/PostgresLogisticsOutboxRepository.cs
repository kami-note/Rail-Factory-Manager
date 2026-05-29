using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class PostgresLogisticsOutboxRepository(LogisticsDbContext db) : ILogisticsOutboxRepository
{
    public async Task AddAsync(LogisticsOutboxMessage message, CancellationToken ct)
    {
        await db.OutboxMessages.AddAsync(message, ct);
    }
}
