using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class PostgresInboundWebhookEventRepository(LogisticsDbContext db)
    : IInboundWebhookEventRepository
{
    public async Task AddAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        db.InboundWebhookEvents.Add(webhookEvent);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string provider, string externalId, CancellationToken cancellationToken = default) =>
        db.InboundWebhookEvents.AsNoTracking()
            .AnyAsync(e => e.Provider == provider && e.ExternalId == externalId, cancellationToken);

    public async Task<IReadOnlyList<InboundWebhookEvent>> GetPendingBatchAsync(
        string tenantId, int batchSize = 50, CancellationToken cancellationToken = default)
    {
        // SKIP LOCKED ensures multiple processor instances don't pick up the same rows
        return await db.InboundWebhookEvents
            .FromSqlRaw("""
                SELECT * FROM logistics_inbound_webhook_events
                WHERE "TenantId" = {0}
                  AND "Status" IN ('Pending', 'Failed')
                ORDER BY "ReceivedAt"
                LIMIT {1}
                FOR UPDATE SKIP LOCKED
                """, tenantId, batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        db.InboundWebhookEvents.Update(webhookEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
