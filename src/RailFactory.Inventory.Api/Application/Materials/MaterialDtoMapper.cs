using RailFactory.Inventory.Api.Api.Responses;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Materials;

internal static class MaterialDtoMapper
{
    public static MaterialResponse ToResponse(Material material) =>
        new(
            material.MaterialCode.Value,
            material.OfficialName,
            material.Description,
            material.UnitOfMeasure,
            material.ProcurementType.ToString(),
            material.Category.ToString(),
            material.Status.ToString(),
            material.Gtin,
            material.Ncm,
            material.ImageUrl,
            material.CreatedBy.Value,
            material.LastModifiedBy.Value,
            material.ReplacedBy?.Value,
            []);
}
