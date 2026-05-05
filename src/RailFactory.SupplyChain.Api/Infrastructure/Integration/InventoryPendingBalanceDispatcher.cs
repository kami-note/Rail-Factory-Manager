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
        
        // Set tenant context for this scope
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
            PendingBalanceRequestedPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<PendingBalanceRequestedPayload>(
                    message.PayloadJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid outbox payload for message {MessageId}.", message.Id);
                message.MarkDeadLetter($"Invalid JSON payload: {ex.Message}");
                continue;
            }

            if (payload is null)
            {
                logger.LogError("Invalid outbox payload for message {MessageId}.", message.Id);
                message.MarkDeadLetter("Invalid JSON payload: payload was null.");
                continue;
            }

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
                        payload.MaterialCode,
                        payload.Quantity,
                        payload.UnitOfMeasure,
                        source = string.IsNullOrWhiteSpace(payload.Source) ? "supply-chain" : payload.Source
                    }
                })
            };

            request.Headers.Add("X-Tenant-Code", tenant.Code);

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "Inventory integration request failed for outbox message {MessageId}.", message.Id);
                MarkTransientFailure(message, ex.Message);
                continue;
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

                continue;
            }

            message.MarkDispatched();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
        string? Source);
}
