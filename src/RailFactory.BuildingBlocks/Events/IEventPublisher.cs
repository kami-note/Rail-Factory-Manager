namespace RailFactory.BuildingBlocks.Events;

public interface IEventPublisher
{
    Task PublishAsync<TPayload>(
        EventEnvelope<TPayload> envelope,
        CancellationToken cancellationToken = default);
}
