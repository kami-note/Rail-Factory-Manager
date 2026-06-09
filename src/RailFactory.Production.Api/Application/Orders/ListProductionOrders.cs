using RailFactory.Production.Api.Application.Ports;
using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Orders;

/// <summary>
/// Lists Production Orders with optional filtering by status and work center.
/// </summary>
public sealed class ListProductionOrders(IProductionOrderRepository repository)
{
    public Task<List<ProductionOrder>> ExecuteAsync(
        ProductionOrderStatus? status,
        Guid? workCenterId,
        CancellationToken cancellationToken,
        string? productCode = null)
        => repository.ListAsync(status, workCenterId, productCode, cancellationToken);
}
