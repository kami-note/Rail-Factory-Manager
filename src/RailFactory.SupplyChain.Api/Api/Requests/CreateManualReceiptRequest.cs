using System.ComponentModel.DataAnnotations;

namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed class CreateManualReceiptRequest
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string ReceiptNumber { get; init; } = string.Empty;

    [Required]
    public Guid SupplierId { get; init; }

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string DocumentNumber { get; init; } = string.Empty;

    [Required]
    public DateOnly ReceiptDate { get; init; }

    [Required]
    [MinLength(1)]
    public List<CreateManualReceiptItemRequest> Items { get; init; } = [];
}

public sealed class CreateManualReceiptItemRequest
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string MaterialCode { get; init; } = string.Empty;

    [Range(0.0001, double.MaxValue)]
    public decimal ExpectedQuantity { get; init; }

    [Required]
    [StringLength(16, MinimumLength = 1)]
    public string UnitOfMeasure { get; init; } = string.Empty;
}
