namespace RailFactory.Production.Api.Application.Ports;

/// <summary>
/// Defines a port to query current material unit costs from the Supply Chain boundary.
/// </summary>
/// <remarks>
/// This interface decouples the Production module from the direct persistence or API of the Supply Chain module,
/// adhering to Hexagonal Architecture principles.
/// </remarks>
public interface IMaterialCostProvider
{
    /// <summary>
    /// Fetches the latest purchase unit costs for the specified material codes.
    /// </summary>
    /// <param name="materialCodes">The collection of material codes to look up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary mapping each material code to its latest unit cost (defaulting to 0 if not found).</returns>
    Task<IReadOnlyDictionary<string, decimal>> GetMaterialCostsAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken = default);
}
