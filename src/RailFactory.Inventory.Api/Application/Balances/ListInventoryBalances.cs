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
    public async Task<List<InventoryBalanceListItemResponse>> ExecuteAsync(
        InventoryBalanceStatus? status,
        InventorySourceType? sourceType,
        CancellationToken cancellationToken)
    {
        var balances = await repository.ListBalancesAsync(status, sourceType, cancellationToken);
        if (balances.Count == 0) return [];

        // ELITE FIX: Bulk fetch materials to eliminate N+1 query pattern.
        var materialCodes = balances.Select(x => x.MaterialCode).Distinct();
        var materials = await materialRepository.GetByCodesAsync(materialCodes, cancellationToken);

        return balances.Select(x =>
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

            var material = materials.TryGetValue(x.MaterialCode, out var m) ? m : null;

            return new InventoryBalanceListItemResponse(
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
            );
        }).ToList();
    }
}
