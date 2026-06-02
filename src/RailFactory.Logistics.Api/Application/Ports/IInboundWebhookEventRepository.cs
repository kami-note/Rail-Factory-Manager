using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Ports;

public interface IInboundWebhookEventRepository
{
    Task AddAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string provider, string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InboundWebhookEvent>> GetPendingBatchAsync(string tenantId, int batchSize = 50, CancellationToken cancellationToken = default);
    Task UpdateAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
