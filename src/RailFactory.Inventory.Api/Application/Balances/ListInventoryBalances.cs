using System.Text.Json;
using RailFactory.Inventory.Api.Api.Responses;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Retrieves a list of inventory balances with enriched material and supplier information.
/// Supports optional filtering by status.
/// </summary>
public sealed class ListInventoryBalances(
    IInventoryRepository repository,
    IMaterialRepository materialRepository)
{
    /// <summary>
    /// Executes the query to list all inventory balances.
    /// </summary>
    /// <remarks>
    /// To ensure operators can view, edit, and manage all registered materials (even when they have no active stock),
    /// this query performs a left-join simulation: it fetches all active stock balances and then appends synthetic
    /// zero-quantity balances for any registered catalog materials that do not have active stock records in the database.
    /// </remarks>
    public async Task<List<InventoryBalanceListItemResponse>> ExecuteAsync(
        InventoryBalanceStatus? status,
        InventorySourceType? sourceType,
        CancellationToken cancellationToken)
    {
        var balances = await repository.ListBalancesAsync(status, sourceType, cancellationToken);
        var allMaterials = await materialRepository.ListAllAsync(cancellationToken);

        // Group materials by code for fast lookup
        var materialsDict = allMaterials.ToDictionary(m => m.MaterialCode.Value, m => m);

        // Output list
        var results = new List<InventoryBalanceListItemResponse>();

        // 1. Process active balances
        foreach (var x in balances)
        {
            string? supplierName = null;
            string? ncm = null;
            string? gtin = null;
            string? originalDescription = null;

            if (!string.IsNullOrWhiteSpace(x.SourceMetadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(x.SourceMetadata);
                    if (doc.RootElement.TryGetProperty("SupplierName", out var prop) ||
                        doc.RootElement.TryGetProperty("supplierName", out prop))
                    {
                        supplierName = prop.GetString();
                    }

                    if (doc.RootElement.TryGetProperty("OriginalDescription", out var descProp) ||
                        doc.RootElement.TryGetProperty("originalDescription", out descProp))
                    {
                        originalDescription = descProp.GetString();
                    }

                    if (doc.RootElement.TryGetProperty("Ncm", out var ncmProp) ||
                        doc.RootElement.TryGetProperty("ncm", out ncmProp))
                    {
                        ncm = ncmProp.GetString();
                    }

                    if (doc.RootElement.TryGetProperty("Gtin", out var gtinProp) ||
                        doc.RootElement.TryGetProperty("gtin", out gtinProp))
                    {
                        gtin = gtinProp.GetString();
                    }
                }
                catch { /* Ignore malformed JSON */ }
            }

            var material = materialsDict.TryGetValue(x.MaterialCode, out var m) ? m : null;

            results.Add(new InventoryBalanceListItemResponse(
                x.Id,
                x.MaterialCode,
                material?.OfficialName ?? originalDescription ?? x.MaterialCode,
                x.Quantity,
                x.UnitOfMeasure,
                x.Status.ToDisplayStatus(),
                x.SourceReference,
                x.LotNumber,
                x.ExpirationDate?.ToString("yyyy-MM-dd"),
                x.SourceType.ToDisplayStatus(),
                supplierName,
                x.CreatedAt,
                material?.Ncm ?? ncm,
                material?.Gtin ?? gtin,
                material?.ImageUrl
            ));
        }

        // 2. Identify materials that have no balances and add a synthetic zero-balance row
        var codesWithBalances = balances.Select(x => x.MaterialCode).ToHashSet();

        // Map sourceType parameter to the expected MaterialCategory
        MaterialCategory? expectedCategory = sourceType switch
        {
            InventorySourceType.Purchase => MaterialCategory.RawMaterial,
            InventorySourceType.Production => MaterialCategory.FinishedGood,
            _ => null
        };

        // If a status filter is active, only include zero-quantity balances if the filter is 'Available'
        var matchesStatusFilter = !status.HasValue || status.Value == InventoryBalanceStatus.Available;

        if (matchesStatusFilter)
        {
            foreach (var material in allMaterials)
            {
                if (expectedCategory.HasValue && material.Category != expectedCategory.Value)
                {
                    continue;
                }

                if (!codesWithBalances.Contains(material.MaterialCode.Value))
                {
                    // Generate a synthetic zero-quantity Available balance
                    results.Add(new InventoryBalanceListItemResponse(
                        Guid.Empty, // Synthetic indicator
                        material.MaterialCode.Value,
                        material.OfficialName,
                        0m,
                        material.UnitOfMeasure,
                        InventoryBalanceStatus.Available.ToDisplayStatus(),
                        $"catalog-init:{material.MaterialCode.Value}",
                        null,
                        null,
                        (material.Category == MaterialCategory.RawMaterial ? InventorySourceType.Purchase : InventorySourceType.Production).ToDisplayStatus(),
                        null,
                        material.CreatedAt,
                        material.Ncm,
                        material.Gtin,
                        material.ImageUrl
                    ));
                }
            }
        }

        // Return ordered by material name
        return results.OrderBy(r => r.MaterialName).ToList();
    }
}
