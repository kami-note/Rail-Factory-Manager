using System.Text.Json;
using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Infrastructure.Persistence;

public sealed class SupplyOutboxStore(SupplyChainDbContext dbContext) : ISupplyOutbox
{
    public async Task EnqueueAsync(string tenantCode, string eventType, object payload, string correlationId, CancellationToken cancellationToken)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var message = new SupplyOutboxMessage(Guid.NewGuid(), tenantCode, eventType, correlationId, payloadJson);
        await dbContext.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
