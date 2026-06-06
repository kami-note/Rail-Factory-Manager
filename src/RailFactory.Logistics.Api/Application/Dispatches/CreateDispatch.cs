using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Dispatches;

public sealed record CreateDispatchInput(
    Guid ShipmentOrderId, Guid CarrierId, Guid VehicleId, Guid DriverPersonId);

public sealed class CreateDispatch(
    IShipmentOrderRepository orders, ICarrierRepository carriers, IDispatchRepository dispatches)
{
    public async Task<Dispatch> ExecuteAsync(CreateDispatchInput input, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(input.ShipmentOrderId, ct)
            ?? throw new KeyNotFoundException($"Shipment order {input.ShipmentOrderId} not found.");

        if (order.Status != ShipmentOrderStatus.ReadyToShip)
            throw new InvalidOperationException("Only ReadyToShip orders can be dispatched.");

        var carrier = await carriers.GetByIdAsync(input.CarrierId, ct)
            ?? throw new KeyNotFoundException($"Carrier {input.CarrierId} not found.");

        var totalKg = order.Items.Sum(i => i.WeightKg * i.Quantity);
        var totalCbm = order.Items.Sum(i => i.VolumeCbm * i.Quantity);
        var freightBrl = Math.Max(totalKg * carrier.RatePerKg, totalCbm * carrier.RatePerCbm);

        var dispatch = Dispatch.Create(order.Id, carrier.Id, input.VehicleId, input.DriverPersonId, freightBrl);
        await dispatches.SaveAsync(dispatch, ct);

        return dispatch;
    }
}
