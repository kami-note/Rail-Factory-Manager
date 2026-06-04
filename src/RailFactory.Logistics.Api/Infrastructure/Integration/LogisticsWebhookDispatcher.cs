using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Logistics.Api.Infrastructure.Persistence;

namespace RailFactory.Logistics.Api.Infrastructure.Integration;

public sealed class LogisticsWebhookDispatcher(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<LogisticsWebhookDispatcher> logger) : BackgroundService
{
    private const int MaxTransientAttempts = 10;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch logistics webhook notifications.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchAllTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var catalogClient = scope.ServiceProvider.GetRequiredService<ITenantCatalogClient>();
        var activeTenants = await catalogClient.ListAllAsync(cancellationToken);

        foreach (var tenant in activeTenants.Where(x => x.IsActive))
        {
            try
            {
                await DispatchTenantBatchAsync(tenant, cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("was not found in configuration"))
            {
                logger.LogDebug("Database for tenant {TenantCode} is not provisioned yet. Skipping outbox dispatch.", tenant.Code);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch webhook notifications for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code, tenant.Locale, tenant.TimeZone, tenant.ConnectionStrings);

        var dbContext = scope.ServiceProvider.GetRequiredService<LogisticsDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pendingMessages = await dbContext.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "logistics_outbox"
                WHERE "EventType" = 'logistics.webhook_notification'
                  AND "DispatchedAt" IS NULL
                  AND "DeadLetteredAt" IS NULL
                ORDER BY "OccurredAt" ASC
                LIMIT 50
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
            await HandleWebhookNotificationAsync(message, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task HandleWebhookNotificationAsync(
        Domain.LogisticsOutboxMessage message,
        CancellationToken cancellationToken)
    {
        WebhookNotificationPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookNotificationPayload>(message.Payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize webhook outbox message {MessageId}.", message.Id);
            message.MarkDeadLetter($"Invalid JSON: {ex.Message}");
            return;
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.WebhookUrl))
        {
            message.MarkDispatched();
            return;
        }

        var externalPayload = new
        {
            payload.TrackingCode,
            payload.NewStatus,
            payload.OrderNumber,
            payload.DispatchId,
            payload.OccurredAt
        };

        try
        {
            var client = httpClientFactory.CreateClient("logistics-webhook");
            using var request = new HttpRequestMessage(HttpMethod.Post, payload.WebhookUrl)
            {
                Content = JsonContent.Create(externalPayload)
            };
            request.Headers.Add("X-Idempotency-Key", message.Id.ToString());

            using var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                message.MarkDispatched();
                logger.LogInformation(
                    "Webhook delivered for dispatch {TrackingCode} → {Status} (HTTP {StatusCode}).",
                    payload.TrackingCode, payload.NewStatus, (int)response.StatusCode);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = $"HTTP {(int)response.StatusCode}: {body[..Math.Min(500, body.Length)]}";

                if (message.AttemptCount + 1 >= MaxTransientAttempts)
                    message.MarkDeadLetter(error);
                else
                    message.MarkTransientFailure(error);

                logger.LogWarning(
                    "Webhook failed for dispatch {TrackingCode}: {Error}.", payload.TrackingCode, error);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var error = ex.Message;
            if (message.AttemptCount + 1 >= MaxTransientAttempts)
                message.MarkDeadLetter(error);
            else
                message.MarkTransientFailure(error);

            logger.LogWarning(ex, "Webhook exception for dispatch {TrackingCode}.", payload.TrackingCode);
        }
    }

    private sealed record WebhookNotificationPayload(
        Guid DispatchId,
        string TrackingCode,
        Guid ShipmentOrderId,
        string? OrderNumber,
        string NewStatus,
        string WebhookUrl,
        string CarrierName,
        DateTimeOffset OccurredAt);
}
