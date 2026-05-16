using System.ComponentModel.DataAnnotations;

namespace RailFactory.Inventory.Api.Api.Requests;

public sealed class ReserveInventoryBalanceRequest
{
    [Required] public Guid EventId { get; init; }
    [Required] public string EventType { get; init; } = string.Empty;
    [Required] public string CorrelationId { get; init; } = string.Empty;
    [Required] public ReserveInventoryBalancePayload Payload { get; init; } = default!;
}

public sealed class ReserveInventoryBalancePayload
{
    [Required] public Guid ProductionOrderId { get; init; }
    [Required] public string OrderNumber { get; init; } = string.Empty;
    [Required] public string MaterialCode { get; init; } = string.Empty;
    [Range(0.0001, double.MaxValue)] public decimal RequiredQuantity { get; init; }
    [Required] public string UnitOfMeasure { get; init; } = string.Empty;
}
