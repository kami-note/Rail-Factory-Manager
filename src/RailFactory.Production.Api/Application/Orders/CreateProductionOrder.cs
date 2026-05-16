using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Creates a new Production Order in Draft status.
/// </summary>
public sealed class CreateProductionOrder(
    IProductionOrderRepository orderRepository,
    IBomRepository bomRepository,
    IWorkCenterRepository workCenterRepository)
{
    public async Task<ProductionOrder> ExecuteAsync(CreateProductionOrderInput input, CancellationToken cancellationToken)
    {
        var bom = await bomRepository.GetByIdAsync(input.BomId, cancellationToken)
            ?? throw new KeyNotFoundException($"BOM '{input.BomId}' not found.");

        var workCenter = await workCenterRepository.GetByIdAsync(input.WorkCenterId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work Center '{input.WorkCenterId}' not found.");

        var orderNumber = await orderRepository.GenerateOrderNumberAsync(cancellationToken);

        var order = ProductionOrder.Create(
            orderNumber,
            bom.ProductCode.Value,
            bom.Id,
            workCenter.Id,
            input.PlannedQuantity);

        await orderRepository.AddAsync(order, cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);

        return order;
    }
}

public sealed record CreateProductionOrderInput(Guid BomId, Guid WorkCenterId, decimal PlannedQuantity);
