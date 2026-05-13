using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Infrastructure.Persistence;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

/// <summary>
/// Background service that dispatches Supply Chain outbox events to the Inventory microservice.
/// </summary>
public sealed class InventoryPendingBalanceDispatcher(
    IServiceProvider serviceProvider,
    ILogger<InventoryPendingBalanceDispatcher> logger) : BackgroundService
{
    private const int MaxTransientAttempts = 10;
    private const string DispatcherEmail = "system-dispatcher@railfactory.local";
    private const string DispatcherName = "Inventory Integration Dispatcher";

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
            // ELITE FIX: Use centralized integration constants to avoid naming mismatches.
            if (message.EventType == IntegrationConstants.Events.ReceiptItemRegistered)
            {
                await HandleRegisteredEventAsync(message, tenant, client, cancellationToken);
            }
            else if (message.EventType == IntegrationConstants.Events.ReceiptItemConferred)
            {
                await HandleConferredEventAsync(message, tenant, client, cancellationToken);
            }
            else if (message.EventType == IntegrationConstants.Events.SupplierMaterialMappingCreated)
            {
                await HandleSupplierMaterialMappingCreatedAsync(message, tenant, client, cancellationToken);
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

        // ELITE FIX: Call correct internal API path using constants.
        var request = new HttpRequestMessage(HttpMethod.Post, IntegrationConstants.ApiPaths.InternalPendingBalances)
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
                    payload.Ncm,
                    payload.Gtin,
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

        var request = new HttpRequestMessage(HttpMethod.Post, IntegrationConstants.ApiPaths.InternalConfirmedBalances)
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

    private async Task HandleSupplierMaterialMappingCreatedAsync(SupplyOutboxMessage message, TenantResolutionResult tenant, HttpClient client, CancellationToken cancellationToken)
    {
        // ELITE FIX: Use primitive DTO instead of record with Value Objects to avoid NotSupportedException during deserialization.
        var payload = DeserializePayload<SupplierMaterialMappingPayload>(message);
        if (payload is null) return;

        var request = new HttpRequestMessage(HttpMethod.Post, IntegrationConstants.ApiPaths.InternalSupplierMaterialMapping)
        {
            Content = JsonContent.Create(new
            {
                eventId = message.Id,
                eventType = message.EventType,
                correlationId = message.CorrelationId,
                payload = new
                {
                    tenantCode = tenant.Code,
                    supplierFiscalId = payload.SupplierFiscalId,
                    supplierProductCode = payload.SupplierProductCode,
                    materialCode = payload.MaterialCode
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
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // ELITE FIX: Catch NotSupportedException which happens when trying to deserialize types without public constructors (like Value Objects).
            logger.LogError(ex, "Invalid outbox payload for message {MessageId}. Type: {TypeName}", message.Id, typeof(T).Name);
            message.MarkDeadLetter($"Invalid JSON payload: {ex.Message}");
            return null;
        }
    }

    private async Task SendIntegrationRequestAsync(SupplyOutboxMessage message, HttpRequestMessage request, string tenantCode, HttpClient client, CancellationToken cancellationToken)
    {
        // ELITE FIX: Propagate tenant and dispatcher identity via trusted headers.
        request.Headers.Add(TenantConstants.TenantCodeHeaderName, tenantCode);
        request.Headers.Add(TenantConstants.UserEmailHeaderName, DispatcherEmail);
        request.Headers.Add(TenantConstants.UserNameHeaderName, DispatcherName);

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
        string? SupplierName,
        string? Ncm,
        string? Gtin);

    private sealed record BalanceConfirmationPayload(
        Guid ReceiptId,
        Guid ReceiptItemId,
        string ReceiptStatus,
        bool IsItemApproved,
        decimal CountedQuantity,
        string? LotNumber,
        DateTimeOffset? ExpirationDate,
        string? Source);

    private sealed record SupplierMaterialMappingPayload(
        string SupplierFiscalId,
        string SupplierProductCode,
        string MaterialCode);
}
