namespace RailFactory.SupplyChain.Api.Application.Ports;

public interface ISupplyOutbox
{
    Task EnqueueAsync(string eventType, object payload, string correlationId, CancellationToken cancellationToken);
}
