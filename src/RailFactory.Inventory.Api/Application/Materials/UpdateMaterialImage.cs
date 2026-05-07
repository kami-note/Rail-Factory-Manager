using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Materials;

/// <summary>
/// Updates the public image URL for a material in the catalog.
/// </summary>
public sealed class UpdateMaterialImage(IMaterialRepository repository)
{
    /// <summary>
    /// Executes the image update.
    /// </summary>
    /// <param name="materialCode">Target material SKU.</param>
    /// <param name="imageUrl">New public image URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated or provisioned.</returns>
    public async Task<bool> ExecuteAsync(string materialCode, string imageUrl, CancellationToken cancellationToken)
    {
        var material = await repository.GetByCodeAsync(materialCode, cancellationToken);
        
        if (material is null)
        {
            // ELITE JIT PROVISIONING: If the material record is missing (but a balance exists in the UI),
            // we create it on the fly to allow enrichment.
            material = Material.Create(
                materialCode,
                officialName: materialCode, 
                description: "Auto-provisioned during image upload.",
                MaterialCategory.RawMaterial,
                MaterialStatus.Draft,
                imageUrl: imageUrl);
            
            await repository.AddAsync(material, cancellationToken);
        }
        else
        {
            material.UpdateImageUrl(imageUrl);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
