using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RailFactory.SupplyChain.Api.Infrastructure.Persistence;

namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

public sealed class InventoryPendingBalanceDispatcher(
    IServiceProvider serviceProvider,
    ILogger<InventoryPendingBalanceDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch supply outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SupplyChainDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("inventory-integration");

        var pendingMessages = await dbContext.OutboxMessages
            .Where(x => x.DispatchedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            var payload = JsonSerializer.Deserialize<PendingBalanceRequestedPayload>(
                message.PayloadJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload is null)
            {
                logger.LogError("Invalid outbox payload for message {MessageId}.", message.Id);
                continue;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "/internal/pending-balances")
            {
                Content = JsonContent.Create(new
                {
                    eventId = message.Id,
                    eventType = message.EventType,
                    correlationId = message.CorrelationId,
                    payload
                })
            };

            request.Headers.Add("X-Tenant-Code", message.TenantCode);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Inventory integration returned status code {StatusCode} for outbox message {MessageId}.", response.StatusCode, message.Id);
                continue;
            }

            message.MarkDispatched();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record PendingBalanceRequestedPayload(
        Guid ReceiptId,
        Guid ReceiptItemId,
        string ReceiptNumber,
        string TenantCode,
        string MaterialCode,
        decimal Quantity,
        string UnitOfMeasure,
        string Source);
}
