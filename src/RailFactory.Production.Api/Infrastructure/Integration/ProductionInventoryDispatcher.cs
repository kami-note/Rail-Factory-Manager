using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Events;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Production.Api.Domain;
using RailFactory.Production.Api.Infrastructure.Persistence;

namespace RailFactory.Production.Api.Infrastructure.Integration;

/// <summary>
/// Background service that dispatches Production outbox events to the Inventory microservice.
/// Processes <c>production_order_released</c> events to reserve stock for each BOM item.
/// </summary>
public sealed class ProductionInventoryDispatcher(
    IServiceProvider serviceProvider,
    ILogger<ProductionInventoryDispatcher> logger) : BackgroundService
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
                logger.LogError(ex, "Failed to dispatch production outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
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
                logger.LogError(ex, "Failed to dispatch production outbox messages for tenant {TenantCode}.", tenant.Code);
            }
        }
    }

    private async Task DispatchTenantBatchAsync(TenantResolutionResult tenant, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var contextAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        contextAccessor.Current = new TenantContext(
            tenant.Code,
            tenant.Locale,
            tenant.TimeZone,
            tenant.ConnectionStrings);

        var dbContext = scope.ServiceProvider.GetRequiredService<ProductionDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("inventory-integration");
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(x => x.DispatchedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            if (message.EventType == IntegrationConstants.ProductionEvents.ProductionOrderReleased)
                await HandleOrderReleasedAsync(message, tenant, client, dbContext, configuration, cancellationToken);
            else
                logger.LogWarning("Unknown production outbox event type {EventType}.", message.EventType);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleOrderReleasedAsync(
        ProductionOutboxMessage message,
        TenantResolutionResult tenant,
        HttpClient client,
        ProductionDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        OrderReleasedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OrderReleasedPayload>(
                message.Payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize production outbox message {MessageId}.", message.Id);
            message.MarkDeadLetter($"Invalid JSON: {ex.Message}");
            return;
        }

        if (payload is null)
        {
            message.MarkDeadLetter("Payload deserialized to null.");
            return;
        }

        var bom = await dbContext.Boms
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == payload.BomId, cancellationToken);

        if (bom is null)
        {
            message.MarkDeadLetter($"BOM '{payload.BomId}' not found for order '{payload.OrderId}'.");
            return;
        }

        var apiKey = configuration["InternalApiKey"];
        var allSucceeded = true;

        foreach (var item in bom.Items)
        {
            var requiredQuantity = item.Quantity * payload.PlannedQuantity;

            var request = new HttpRequestMessage(HttpMethod.Post, IntegrationConstants.ApiPaths.InternalReserveBalances)
            {
                Content = JsonContent.Create(new
                {
                    eventId = Guid.NewGuid(),
                    eventType = IntegrationConstants.ProductionEvents.ProductionOrderReleased,
                    correlationId = message.Id.ToString(),
                    payload = new
                    {
                        productionOrderId = payload.OrderId,
                        orderNumber = payload.OrderNumber,
                        materialCode = item.MaterialCode.Value,
                        requiredQuantity,
                        unitOfMeasure = item.UnitOfMeasure
                    }
                })
            };

            request.Headers.Add(TenantConstants.TenantCodeHeaderName, tenant.Code);
            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("X-Internal-Key", apiKey);

            try
            {
                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogWarning("Inventory reserve failed for material {MaterialCode} in order {OrderNumber}. Status: {Status}. Body: {Body}",
                        item.MaterialCode.Value, payload.OrderNumber, response.StatusCode, body);
                    allSucceeded = false;
                    break;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "Inventory reserve request failed for outbox message {MessageId}.", message.Id);
                allSucceeded = false;
                break;
            }
        }

        if (allSucceeded)
        {
            message.MarkDispatched();
            logger.LogInformation("Dispatched production_order_released for order {OrderNumber}.", payload.OrderNumber);
        }
        else if (message.AttemptCount + 1 >= MaxTransientAttempts)
        {
            message.MarkDeadLetter("Max transient retry attempts exceeded.");
        }
        else
        {
            message.MarkTransientFailure("Inventory reserve request failed. Will retry.");
        }
    }

    private sealed record OrderReleasedPayload(
        Guid OrderId,
        string OrderNumber,
        string ProductCode,
        Guid BomId,
        Guid WorkCenterId,
        decimal PlannedQuantity,
        DateTimeOffset OccurredAt);
}
