using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Shipments;

public sealed record AddShipmentItemInput(
    Guid OrderId, string MaterialCode, decimal Quantity,
    string UnitOfMeasure, decimal WeightKg, decimal VolumeCbm);

public sealed class AddShipmentItem(IShipmentOrderRepository orders)
{
    public async Task<ShipmentItem> ExecuteAsync(AddShipmentItemInput input, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(input.OrderId, ct)
            ?? throw new KeyNotFoundException($"Shipment order {input.OrderId} not found.");

        var item = order.AddItem(input.MaterialCode, input.Quantity,
            input.UnitOfMeasure, input.WeightKg, input.VolumeCbm);

        await orders.AddItemDirectAsync(order.Id, item, order.UpdatedAt, ct);
        return item;
    }
}
