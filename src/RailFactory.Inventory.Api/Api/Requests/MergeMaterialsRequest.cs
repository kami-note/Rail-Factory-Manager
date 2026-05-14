using System.ComponentModel.DataAnnotations;

namespace RailFactory.Inventory.Api.Api.Requests;

/// <summary>
/// Request payload for merging two materials.
/// </summary>
public sealed record MergeMaterialsRequest(
    [Required] string ObsoleteMaterialCode,
    [Required] string OfficialMaterialCode);
