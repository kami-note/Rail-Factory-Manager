using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Ports;

public interface IShipmentOrderRepository
{
    Task<ShipmentOrder?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<ShipmentOrder>> ListAsync(ShipmentOrderStatus? status, CancellationToken ct);
    Task SaveAsync(ShipmentOrder order, CancellationToken ct);
    Task AddItemDirectAsync(Guid orderId, ShipmentItem item, DateTimeOffset orderUpdatedAt, CancellationToken ct);
}
