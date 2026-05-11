namespace RailFactory.SupplyChain.Api.Application.Ports;

public sealed record MaterialMetadata(
    string MaterialCode,
    string OfficialName,
    string UnitOfMeasure,
    string? ImageUrl);

public sealed record CreateMaterialInput(
    string MaterialCode,
    string OfficialName,
    string Description,
    string UnitOfMeasure,
    string ProcurementType,
    string Category,
    string? Gtin,
    string? Ncm);

/// <summary>
/// Port to retrieve material metadata and manage materials in the Inventory domain.
/// </summary>
public interface IInventoryMaterialService
{
    /// <summary>
    /// Fetches metadata for a list of material codes.
    /// </summary>
    Task<IDictionary<string, MaterialMetadata>> GetMaterialsByCodesAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new material in the inventory catalog.
    /// </summary>
    Task<MaterialMetadata> CreateMaterialAsync(CreateMaterialInput input, CancellationToken cancellationToken);
}
