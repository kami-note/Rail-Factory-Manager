using System.Text.Json;
using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Releases a Production Order, validating BOM and Work Center active status,
/// and persisting a <c>production_order_released</c> outbox message for downstream consumption.
/// </summary>
public sealed class ReleaseProductionOrder(
    IProductionOrderRepository orderRepository,
    IBomRepository bomRepository,
    IWorkCenterRepository workCenterRepository)
{
    public async Task ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Production Order '{orderId}' not found.");

        var bom = await bomRepository.GetByIdAsync(order.BomId, cancellationToken)
            ?? throw new InvalidOperationException($"BOM '{order.BomId}' referenced by order '{orderId}' not found.");

        if (bom.Status != BomStatus.Active)
            throw new InvalidOperationException($"Cannot release order: BOM '{bom.Id}' is in status '{bom.Status}'. Only Active BOMs can be used.");

        var workCenter = await workCenterRepository.GetByIdAsync(order.WorkCenterId, cancellationToken)
            ?? throw new InvalidOperationException($"Work Center '{order.WorkCenterId}' referenced by order '{orderId}' not found.");

        if (workCenter.Status != WorkCenterStatus.Active)
            throw new InvalidOperationException($"Cannot release order: Work Center '{workCenter.Code}' is inactive.");

        order.Release();

        var payload = JsonSerializer.Serialize(new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            productCode = order.ProductCode.Value,
            bomId = order.BomId,
            workCenterId = order.WorkCenterId,
            plannedQuantity = order.PlannedQuantity,
            occurredAt = DateTimeOffset.UtcNow
        });

        var outboxMessage = ProductionOutboxMessage.Create("production_order_released", payload);

        await orderRepository.AddOutboxMessageAsync(outboxMessage, cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);
    }
}
