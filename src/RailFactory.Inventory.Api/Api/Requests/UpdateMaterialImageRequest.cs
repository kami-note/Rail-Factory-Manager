using System.ComponentModel.DataAnnotations;

namespace RailFactory.Inventory.Api.Api.Requests;

public sealed class UpdateMaterialImageRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string ImageUrl { get; init; } = string.Empty;
}
