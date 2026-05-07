using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.SupplyChain.Api.Infrastructure.Persistence;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

public sealed class InventoryPendingBalanceDispatcher(
    IServiceProvider serviceProvider,
    ILogger<InventoryPendingBalanceDispatcher> logger) : BackgroundService
{
    private const int MaxTransientAttempts = 10;

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
                logger.LogError(ex, "Failed to dispatch supply outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch supply outbox messages for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new RailFactory.BuildingBlocks.Tenancy.TenantContext(
            tenant.Code,
            tenant.Locale,
            tenant.TimeZone,
            tenant.ConnectionStrings);

        var dbContext = scope.ServiceProvider.GetRequiredService<SupplyChainDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("inventory-integration");

        var pendingMessages = await dbContext.OutboxMessages
            .Where(x => x.Status == SupplyOutboxMessageStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            if (message.EventType == "supply.receipt_item_registered")
            {
                await HandleRegisteredEventAsync(message, tenant, client, cancellationToken);
            }
            else if (message.EventType == "supply.receipt_item_conferred")
            {
                await HandleConferredEventAsync(message, tenant, client, cancellationToken);
            }
            else
            {
                logger.LogWarning("Unknown event type {EventType} in supply outbox.", message.EventType);
                message.MarkDeadLetter($"Unknown event type: {message.EventType}");
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleRegisteredEventAsync(SupplyOutboxMessage message, TenantResolutionResult tenant, HttpClient client, CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<PendingBalanceRequestedPayload>(message);
        if (payload is null) return;

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/pending-balances")
        {
            Content = JsonContent.Create(new
            {
                eventId = message.Id,
                eventType = message.EventType,
                correlationId = message.CorrelationId,
                payload = new
                {
                    tenantCode = tenant.Code,
                    payload.ReceiptId,
                    payload.ReceiptItemId,
                    payload.ReceiptNumber,
                    payload.SupplierName,
                    payload.MaterialCode,
                    payload.Quantity,
                    payload.UnitOfMeasure,
                    payload.UnitPrice,
                    payload.OriginalDescription,
                    payload.AccessKey,
                    source = string.IsNullOrWhiteSpace(payload.Source) ? "supply-chain" : payload.Source
                }
            })
        };

        await SendIntegrationRequestAsync(message, request, tenant.Code, client, cancellationToken);
    }

    private async Task HandleConferredEventAsync(SupplyOutboxMessage message, TenantResolutionResult tenant, HttpClient client, CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<BalanceConfirmationPayload>(message);
        if (payload is null) return;

        var request = new HttpRequestMessage(HttpMethod.Post, "/internal/confirmed-balances")
        {
            Content = JsonContent.Create(new
            {
                eventId = message.Id,
                eventType = message.EventType,
                correlationId = message.CorrelationId,
                payload = new
                {
                    tenantCode = tenant.Code,
                    payload.ReceiptId,
                    payload.ReceiptItemId,
                    status = payload.ReceiptStatus,
                    isApproved = payload.IsItemApproved,
                    payload.CountedQuantity,
                    payload.LotNumber,
                    payload.ExpirationDate,
                    source = string.IsNullOrWhiteSpace(payload.Source) ? "supply-chain" : payload.Source
                }
            })
        };

        await SendIntegrationRequestAsync(message, request, tenant.Code, client, cancellationToken);
    }

    private T? DeserializePayload<T>(SupplyOutboxMessage message) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                message.PayloadJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid outbox payload for message {MessageId}.", message.Id);
            message.MarkDeadLetter($"Invalid JSON payload: {ex.Message}");
            return null;
        }
    }

    private async Task SendIntegrationRequestAsync(SupplyOutboxMessage message, HttpRequestMessage request, string tenantCode, HttpClient client, CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Tenant-Code", tenantCode);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Inventory integration request failed for outbox message {MessageId}.", message.Id);
            MarkTransientFailure(message, ex.Message);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Inventory integration returned status code {StatusCode} for outbox message {MessageId}.", response.StatusCode, message.Id);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                message.MarkDeadLetter($"Inventory returned 400 Bad Request. Body: {errorBody}");
            }
            else
            {
                MarkTransientFailure(message, $"Inventory returned {(int)response.StatusCode} {response.StatusCode}.");
            }

            return;
        }

        message.MarkDispatched();
    }

    private static void MarkTransientFailure(SupplyOutboxMessage message, string error)
    {
        if (message.AttemptCount + 1 >= MaxTransientAttempts)
        {
            message.MarkDeadLetter($"Transient retry limit exceeded. Last error: {error}");
            return;
        }

        message.MarkTransientFailure(error);
    }

    private sealed record PendingBalanceRequestedPayload(
        Guid ReceiptId,
        Guid ReceiptItemId,
        string ReceiptNumber,
        string MaterialCode,
        decimal Quantity,
        string UnitOfMeasure,
        decimal? UnitPrice,
        string? OriginalDescription,
        string? AccessKey,
        string? Source,
        string? SupplierName);

    private sealed record BalanceConfirmationPayload(
        Guid ReceiptId,
        Guid ReceiptItemId,
        string ReceiptStatus,
        bool IsItemApproved,
        decimal CountedQuantity,
        string? LotNumber,
        DateTimeOffset? ExpirationDate,
        string? Source);
}
