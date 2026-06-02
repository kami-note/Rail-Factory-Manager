using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Ports;

public interface IInboundWebhookHandler
{
    string Provider { get; }
    Task HandleAsync(InboundWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
