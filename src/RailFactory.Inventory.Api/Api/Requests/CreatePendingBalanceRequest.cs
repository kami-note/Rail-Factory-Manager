using System.ComponentModel.DataAnnotations;

namespace RailFactory.Inventory.Api.Api.Requests;

public sealed class CreatePendingBalanceRequest
{
    [Required]
    public Guid EventId { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string EventType { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string CorrelationId { get; init; } = string.Empty;

    [Required]
    public CreatePendingBalancePayload Payload { get; init; } = new();
}

public sealed class CreatePendingBalancePayload
{
    [Required]
    public Guid ReceiptId { get; init; }

    [Required]
    public Guid ReceiptItemId { get; init; }

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string ReceiptNumber { get; init; } = string.Empty;

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string TenantCode { get; init; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string MaterialCode { get; init; } = string.Empty;

    [Range(0.0001, double.MaxValue)]
    public decimal Quantity { get; init; }

    [Required]
    [StringLength(16, MinimumLength = 1)]
    public string UnitOfMeasure { get; init; } = string.Empty;

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Source { get; init; } = string.Empty;
}
