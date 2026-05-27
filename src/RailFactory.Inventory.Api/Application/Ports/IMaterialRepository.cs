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
    /// Retrieves a material by GTIN/EAN when present.
    /// </summary>
    Task<Material?> GetByGtinAsync(string gtin, CancellationToken cancellationToken);

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
    /// Searches for materials by a search term (matches code, name, or GTIN).
    /// </summary>
    /// <param name="term">The search term.</param>
    /// <param name="category">Optional category filter (RawMaterial or FinishedGood).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching materials.</returns>
    Task<List<Material>> SearchAsync(string term, MaterialCategory? category, CancellationToken cancellationToken);

    /// <summary>
    /// Upserts a supplier material hint.
    /// </summary>
    /// <param name="hint">The supplier material hint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpsertSupplierMaterialHintAsync(SupplierMaterialHint hint, CancellationToken cancellationToken);

    /// <summary>
    /// Searches for material suggestions based on various criteria.
    /// </summary>
    /// <param name="gtin">Optional GTIN.</param>
    /// <param name="ncm">Optional NCM.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="supplierFiscalId">Optional supplier fiscal ID.</param>
    /// <param name="supplierProductCode">Optional supplier product code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of material suggestions.</returns>
    Task<List<SupplierMaterialHintResult>> GetSuggestionsAsync(
        string? gtin, 
        string? ncm, 
        string? description, 
        string? supplierFiscalId, 
        string? supplierProductCode, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists all changes to the underlying storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
