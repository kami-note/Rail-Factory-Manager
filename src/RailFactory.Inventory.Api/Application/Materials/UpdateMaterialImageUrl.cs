using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Materials;

public sealed class UpdateMaterialImageUrl(IMaterialRepository materialRepository)
{
    public async Task ExecuteAsync(string materialCode, string imageUrl, CancellationToken cancellationToken)
    {
        var normalizedCode = materialCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Material code is required.");
        }

        var normalizedUrl = imageUrl?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            throw new InvalidOperationException("Image URL is required.");
        }

        var material = await materialRepository.GetByCodeAsync(normalizedCode, cancellationToken);
        if (material is null)
        {
            throw new KeyNotFoundException($"Material '{normalizedCode}' was not found.");
        }

        material.UpdateImageUrl(normalizedUrl);
        await materialRepository.SaveChangesAsync(cancellationToken);
    }
}
