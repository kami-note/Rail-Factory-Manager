using RailFactory.BuildingBlocks.Presentation;
using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Materials;

/// <summary>
/// Searches the material catalog for standardized products.
/// </summary>
public sealed class SearchMaterials(IMaterialRepository repository)
{
    /// <summary>
    /// Executes a search for materials matching the provided term.
    /// </summary>
    /// <param name="term">Search term (code, name, or GTIN).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of search results formatted for presentation.</returns>
    public async Task<List<MaterialSearchResult>> ExecuteAsync(string term, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return new List<MaterialSearchResult>();
        }

        var results = await repository.SearchAsync(term, cancellationToken);

        return results.Select(m => new MaterialSearchResult(
            m.MaterialCode.Value,
            m.OfficialName,
            m.Description,
            m.ImageUrl,
            m.Category.ToString(),
            m.Gtin,
            m.Ncm,
            m.UnitOfMeasure
        )).ToList();
    }
}

/// <summary>
/// Represents a material found during a catalog search.
/// </summary>
public sealed record MaterialSearchResult(
    string MaterialCode,
    string OfficialName,
    string Description,
    string? ImageUrl,
    string Category,
    string? Gtin,
    string? Ncm,
    string UnitOfMeasure);
