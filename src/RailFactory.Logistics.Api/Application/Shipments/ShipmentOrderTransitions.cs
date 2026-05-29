using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Application.Shipments;

public sealed class StartPicking(IShipmentOrderRepository orders)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Shipment order {id} not found.");
        order.StartPicking();
        await orders.SaveAsync(order, ct);
    }
}

public sealed class StartPacking(IShipmentOrderRepository orders)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Shipment order {id} not found.");
        order.StartPacking();
        await orders.SaveAsync(order, ct);
    }
}

public sealed class MarkReadyToShip(IShipmentOrderRepository orders)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Shipment order {id} not found.");
        order.MarkReadyToShip();
        await orders.SaveAsync(order, ct);
    }
}

public sealed class CancelShipmentOrder(IShipmentOrderRepository orders)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Shipment order {id} not found.");
        order.Cancel();
        await orders.SaveAsync(order, ct);
    }
}

public sealed class ListShipmentOrders(IShipmentOrderRepository orders)
{
    public Task<List<Domain.ShipmentOrder>> ExecuteAsync(Domain.ShipmentOrderStatus? status, CancellationToken ct)
        => orders.ListAsync(status, ct);
}
