using System.Text.Json;
using RailFactory.BuildingBlocks.Events;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Completes a Production Order after verifying a passed quality inspection exists.
/// If no consumption has been recorded manually, automatically backflushes the BOM
/// (registers planned quantities for every BOM item) before completing.
/// Enqueues a production_order_completed outbox entry so Inventory can convert
/// reserved stock into consumed (ledger: production_consumed).
/// </summary>
public sealed class CompleteProductionOrder(
    IProductionOrderRepository orderRepository,
    IBomRepository bomRepository,
    IExecutionRepository executionRepository)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{orderId}' not found.");

        var consumptions = await executionRepository.GetConsumptionByOrderAsync(orderId, cancellationToken);

        // Backflush: if no consumption recorded, auto-register from BOM at planned quantities.
        if (consumptions.Count == 0)
        {
            var bom = await bomRepository.GetByIdAsync(order.BomId, cancellationToken)
                ?? throw new InvalidOperationException($"BOM '{order.BomId}' not found for order '{orderId}'.");

            foreach (var item in bom.Items)
            {
                var record = ConsumptionRecord.Create(
                    orderId,
                    item.MaterialCode.Value,
                    item.Quantity * order.PlannedQuantity,
                    item.UnitOfMeasure);
                await executionRepository.AddConsumptionAsync(record, cancellationToken);
            }

            // Reload after backflush so payload is accurate
            consumptions = await executionRepository.GetConsumptionByOrderAsync(orderId, cancellationToken);
        }

        var latestInspection = await executionRepository.GetLatestInspectionAsync(orderId, cancellationToken);
        var inspectionPassed = latestInspection?.Result == InspectionResult.Passed;

        order.Complete(inspectionPassed);

        var scraps = await executionRepository.GetScrapByOrderAsync(orderId, cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            productCode = order.ProductCode.Value,
            producedQuantity = order.PlannedQuantity,
            consumptions = consumptions
                .GroupBy(c => c.MaterialCode.Value)
                .Select(g => new { materialCode = g.Key, totalQuantity = g.Sum(c => c.ConsumedQuantity), unitOfMeasure = g.First().UnitOfMeasure }),
            totalScrapQuantity = scraps.Sum(s => s.ScrapQuantity)
        });

        var outboxMessage = ProductionOutboxMessage.Create(
            IntegrationConstants.ProductionEvents.ProductionOrderCompleted, payload);
        await orderRepository.AddOutboxMessageAsync(outboxMessage, cancellationToken);

        await orderRepository.SaveChangesAsync(cancellationToken);
    }
}
