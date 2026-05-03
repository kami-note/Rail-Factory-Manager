using System.ComponentModel.DataAnnotations;

namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed class CreateSupplierRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string FiscalId { get; init; } = string.Empty;

    [Required]
    [StringLength(160, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}
