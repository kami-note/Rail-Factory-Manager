namespace RailFactory.SupplyChain.Api.Application.Ports;

public sealed record MaterialMetadata(
    string MaterialCode,
    string OfficialName,
    string? ImageUrl);

/// <summary>
/// Port to retrieve material metadata from the Inventory domain.
/// </summary>
public interface IInventoryMaterialService
{
    /// <summary>
    /// Fetches metadata for a list of material codes.
    /// </summary>
    Task<IDictionary<string, MaterialMetadata>> GetMaterialsByCodesAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken);
}
