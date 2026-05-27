using System.Text.Json;
using RabbitMQ.Client;

namespace RailFactory.BuildingBlocks.Events;

/// <summary>
/// Singleton publisher that sends <see cref="RabbitMqEnvelope"/> messages to a RabbitMQ
/// direct exchange. Designed to be shared across the application — one instance per exchange.
///
/// <b>Channel management:</b> a single <see cref="IChannel"/> is created lazily on the first
/// publish and reused for every subsequent call. If the channel is closed (e.g. after a
/// transient broker error), it is transparently recreated on the next <see cref="PublishAsync"/>
/// invocation. All channel lifecycle operations are protected by a <see cref="SemaphoreSlim"/>
/// to prevent concurrent recreation races while keeping the hot path (healthy channel) lock-free.
///
/// <b>Publisher confirms:</b> the channel is created with
/// <see cref="CreateChannelOptions.PublisherConfirmationsEnabled"/> and
/// <see cref="CreateChannelOptions.PublisherConfirmationTrackingEnabled"/> set to <c>true</c>.
/// In this mode, <see cref="IChannel.BasicPublishAsync"/> itself awaits broker acknowledgement
/// before returning, upgrading the delivery guarantee from fire-and-forget to at-least-once
/// at the transport layer without any separate confirm-wait call.
///
/// <b>Exchange declaration:</b> the exchange is declared on every channel creation so this
/// publisher is self-sufficient regardless of whether another service's topology initializer
/// has already run. <see cref="IChannel.ExchangeDeclareAsync"/> is idempotent for matching
/// parameters.
/// </summary>
public sealed class RabbitMqPublisher : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly string _exchange;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // volatile so the fast-path read in GetChannelAsync sees channel-null writes
    // from PublishAsync error-handling without entering the semaphore.
    private volatile IChannel? _channel;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    // Enables AMQP Confirm Select mode. With PublisherConfirmationTrackingEnabled=true,
    // BasicPublishAsync awaits broker ACK internally before returning — no separate wait needed.
    private static readonly CreateChannelOptions ConfirmChannelOptions =
        new(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);

    /// <param name="connection">Shared RabbitMQ connection (registered as Singleton by Aspire).</param>
    /// <param name="exchange">Name of the direct exchange to publish to.</param>
    public RabbitMqPublisher(IConnection connection, string exchange)
    {
        _connection = connection;
        _exchange = exchange;
    }

    /// <summary>
    /// Returns the open, confirm-mode channel — creating or recreating it when necessary.
    /// Uses double-checked locking: the fast path is entirely lock-free when the channel is healthy.
    /// </summary>
    private async ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        // Fast path — channel is healthy; return immediately without acquiring the lock.
        if (_channel is { IsOpen: true })
            return _channel;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock in case another thread already rebuilt it.
            if (_channel is { IsOpen: true })
                return _channel;

            if (_channel is not null)
            {
                try { await _channel.CloseAsync(CancellationToken.None); } catch { /* best-effort */ }
                _channel.Dispose();
                _channel = null;
            }

            var ch = await _connection.CreateChannelAsync(
                ConfirmChannelOptions,
                cancellationToken);

            // Declare the exchange on channel creation so this publisher is independent of
            // external topology initializers. Idempotent for matching parameters.
            await ch.ExchangeDeclareAsync(
                exchange: _exchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            _channel = ch;
            return _channel;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Publishes the <paramref name="envelope"/> to the exchange and awaits broker confirmation.
    /// Because the channel was created with <see cref="CreateChannelOptions.PublisherConfirmationTrackingEnabled"/>,
    /// <see cref="IChannel.BasicPublishAsync"/> itself blocks until the broker ACKs the message.
    /// On any failure the channel reference is cleared so the next call rebuilds it cleanly.
    /// </summary>
    public async Task PublishAsync(string routingKey, RabbitMqEnvelope envelope, CancellationToken cancellationToken)
    {
        var channel = await GetChannelAsync(cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = envelope.EventId.ToString(),
            CorrelationId = envelope.CorrelationId,
            Timestamp = new AmqpTimestamp(envelope.OccurredAt.ToUnixTimeSeconds()),
        };

        try
        {
            // With PublisherConfirmationTrackingEnabled, this call awaits broker ACK internally.
            await channel.BasicPublishAsync(
                exchange: _exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Clear the channel reference so the next PublishAsync recreates it cleanly
            // instead of attempting to reuse a potentially broken channel.
            _channel = null;
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            try { await _channel.CloseAsync(CancellationToken.None); } catch { /* best-effort */ }
            _channel.Dispose();
        }

        _semaphore.Dispose();
    }
}
