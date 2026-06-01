using System.Threading.Channels;

namespace RailFactory.Inventory.Api.Infrastructure;

/// <summary>
/// In-memory pub/sub broadcaster for real-time inventory alerts (RF-36).
/// SSE clients subscribe; alert publishers call <see cref="PublishAsync"/>.
/// </summary>
public sealed class AlertBroadcaster
{
    private readonly List<Channel<AlertEvent>> _subscribers = [];
    private readonly Lock _lock = new();

    public ChannelReader<AlertEvent> Subscribe()
    {
        var channel = Channel.CreateBounded<AlertEvent>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_lock)
            _subscribers.Add(channel);

        return channel.Reader;
    }

    public void Unsubscribe(ChannelReader<AlertEvent> reader)
    {
        lock (_lock)
            _subscribers.RemoveAll(c => c.Reader == reader);
    }

    public async Task PublishAsync(AlertEvent alert)
    {
        List<Channel<AlertEvent>> snapshot;
        lock (_lock)
            snapshot = [.. _subscribers];

        foreach (var ch in snapshot)
            await ch.Writer.WriteAsync(alert).ConfigureAwait(false);
    }
}

public sealed record AlertEvent(
    string AlertType,
    string Message,
    string? MaterialCode,
    decimal? CurrentQuantity,
    DateTimeOffset OccurredAt);
