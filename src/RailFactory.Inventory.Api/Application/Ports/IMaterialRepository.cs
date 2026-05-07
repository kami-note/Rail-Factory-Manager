using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Ports;

/// <summary>
/// Port for Material catalog persistence.
/// </summary>
public interface IMaterialRepository
{
    /// <summary>
    /// Retrieves a material by its unique code.
    /// </summary>
    /// <param name="materialCode">The SKU or material code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The material or null if not found.</returns>
    Task<Material?> GetByCodeAsync(string materialCode, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves multiple materials by their unique codes in a single query.
    /// </summary>
    /// <param name="materialCodes">List of codes to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of materials keyed by their code.</returns>
    Task<Dictionary<string, Material>> GetByCodesAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new material to the catalog.
    /// </summary>
    /// <param name="material">The material aggregate root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Material material, CancellationToken cancellationToken);

    /// <summary>
    /// Persists all changes to the underlying storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
