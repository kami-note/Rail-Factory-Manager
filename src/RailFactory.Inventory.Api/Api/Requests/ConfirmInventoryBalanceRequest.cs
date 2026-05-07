using System.ComponentModel.DataAnnotations;

namespace RailFactory.Inventory.Api.Api.Requests;

public sealed class ConfirmInventoryBalanceRequest
{
    [Required]
    public Guid EventId { get; init; }

    [Required]
    public string EventType { get; init; } = string.Empty;

    [Required]
    public string CorrelationId { get; init; } = string.Empty;

    [Required]
    public ConfirmInventoryBalancePayload Payload { get; init; } = default!;
}

public sealed class ConfirmInventoryBalancePayload
{
    [Required]
    public Guid ReceiptId { get; init; }

    [Required]
    public Guid ReceiptItemId { get; init; }

    [Required]
    public string Status { get; init; } = string.Empty;

    public bool IsApproved { get; init; }

    [Range(0, double.MaxValue)]
    public decimal CountedQuantity { get; init; }

    public string? LotNumber { get; init; }

    public DateTimeOffset? ExpirationDate { get; init; }
}
