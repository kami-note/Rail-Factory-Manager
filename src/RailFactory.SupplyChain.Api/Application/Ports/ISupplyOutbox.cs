namespace RailFactory.SupplyChain.Api.Application.Ports;

public interface ISupplyOutbox
{
    Task EnqueueAsync(string tenantCode, string eventType, object payload, string correlationId, CancellationToken cancellationToken);
}
