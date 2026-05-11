using RailFactory.Inventory.Api.Api.Responses;
using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Materials;

/// <summary>
/// Returns real catalog details for one Inventory material.
/// </summary>
public sealed class GetMaterialDetails(IMaterialRepository repository)
{
    public async Task<MaterialResponse?> ExecuteAsync(string materialCode, CancellationToken cancellationToken)
    {
        var material = await repository.GetByCodeAsync(materialCode, cancellationToken);
        return material is null ? null : MaterialDtoMapper.ToResponse(material);
    }
}
