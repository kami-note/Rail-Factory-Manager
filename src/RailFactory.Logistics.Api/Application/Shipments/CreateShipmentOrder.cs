using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Shipments;

public sealed record CreateShipmentOrderInput(
    Guid? ProductionOrderRef,
    string? Notes,
    decimal? DeliveryLatitudeDeg = null,
    decimal? DeliveryLongitudeDeg = null,
    string? DeliveryCity = null);

public sealed class CreateShipmentOrder(IShipmentOrderRepository orders)
{
    public async Task<ShipmentOrder> ExecuteAsync(CreateShipmentOrderInput input, CancellationToken ct)
    {
        var order = ShipmentOrder.Create(
            input.ProductionOrderRef, input.Notes,
            input.DeliveryLatitudeDeg, input.DeliveryLongitudeDeg, input.DeliveryCity);
        await orders.SaveAsync(order, ct);
        return order;
    }
}
