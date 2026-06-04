using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Ports;

/// <summary>
/// Persistence port for the ProductionOrder aggregate.
/// </summary>
public interface IProductionOrderRepository
{
    Task AddAsync(ProductionOrder order, CancellationToken cancellationToken);
    Task<ProductionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<ProductionOrder>> ListAsync(ProductionOrderStatus? status, Guid? workCenterId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists an outbox message alongside the order state change in the same transaction.
    /// </summary>
    Task AddOutboxMessageAsync(ProductionOutboxMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the next sequential order number for the current year (e.g., OP-2026-0042).
    /// </summary>
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns true if any Released or InExecution orders reference the given BOM.
    /// Used to guard BOM version deactivation.
    /// </summary>
    Task<bool> HasActiveOrdersForBomAsync(Guid bomId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
