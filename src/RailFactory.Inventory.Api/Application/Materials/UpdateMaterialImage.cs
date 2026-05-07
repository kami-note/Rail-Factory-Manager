using System.Text.Json;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Materials;

/// <summary>
/// Updates the public image URL for a material in the catalog.
/// </summary>
public sealed class UpdateMaterialImage(
    IMaterialRepository repository,
    IInventoryRepository inventoryRepository)
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
        var code = MaterialCode.From(materialCode);
        var material = await repository.GetByCodeAsync(code, cancellationToken);
        
        if (material is null)
        {
            // ELITE JIT PROVISIONING: Try to recover the official name from existing balances
            var latestBalance = await inventoryRepository.GetLatestBalanceByMaterialCodeAsync(code, cancellationToken);
            var officialName = code.Value;
            
            if (latestBalance?.SourceMetadata != null)
            {
                try 
                {
                    using var doc = JsonDocument.Parse(latestBalance.SourceMetadata);
                    if (doc.RootElement.TryGetProperty("OriginalDescription", out var descProp))
                    {
                        officialName = descProp.GetString() ?? code.Value;
                    }
                }
                catch { /* Ignore parsing errors, fallback to SKU */ }
            }

            material = Material.Create(
                code,
                officialName: officialName, 
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

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            // If another request created the material concurrently, we fetch it and update the image URL instead.
            var existingMaterial = await repository.GetByCodeAsync(materialCode, cancellationToken);
            if (existingMaterial is not null)
            {
                existingMaterial.UpdateImageUrl(imageUrl);
                await repository.SaveChangesAsync(cancellationToken);
            }
        }

        return true;
    }

    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        // Simple heuristic for Postgres/EF Core unique constraint violation
        var message = ex.ToString();
        return message.Contains("23505") || message.Contains("unique constraint");
    }
}
