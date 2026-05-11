using System.ComponentModel.DataAnnotations;

namespace RailFactory.Inventory.Api.Api.Requests;

public sealed class CreateSupplierMaterialMappingRequest
{
    [Required]
    public Guid EventId { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string EventType { get; init; } = string.Empty;

    [Required]
    public CreateSupplierMaterialMappingPayload Payload { get; init; } = new();
}

public sealed class CreateSupplierMaterialMappingPayload
{
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string SupplierFiscalId { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string SupplierProductCode { get; init; } = string.Empty;

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string MaterialCode { get; init; } = string.Empty;
}
