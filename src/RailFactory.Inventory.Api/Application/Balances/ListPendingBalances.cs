using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Retrieves a list of all pending inventory balances with enriched material and supplier information.
/// </summary>
public sealed class ListPendingBalances(
    IInventoryRepository repository,
    IMaterialRepository materialRepository)
{
    public async Task<List<PendingBalanceListItemResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var balances = await repository.ListPendingBalancesAsync(cancellationToken);
        if (balances.Count == 0) return [];

        // ELITE FIX: Bulk fetch materials to eliminate N+1 query pattern.
        var materialCodes = balances.Select(x => x.MaterialCode).Distinct();
        var materials = await materialRepository.GetByCodesAsync(materialCodes, cancellationToken);

        return balances.Select(x =>
        {
            string? supplierName = null;
            string? ncm = null;
            string? gtin = null;

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

            return new PendingBalanceListItemResponse(
                x.Id,
                x.MaterialCode,
                material?.OfficialName ?? x.MaterialCode,
                x.Quantity,
                x.UnitOfMeasure,
                x.Status.ToString(),
                x.SourceReference,
                x.LotNumber,
                x.ExpirationDate?.ToString("yyyy-MM-dd"),
                x.SourceType.ToString(),
                supplierName,
                x.SourceMetadata,
                x.CreatedAt,
                material?.Ncm ?? ncm,
                material?.Gtin ?? gtin
            );
        }).ToList();
    }
}

public record PendingBalanceListItemResponse(
    Guid Id,
    string MaterialCode,
    string MaterialName,
    decimal Quantity,
    string UnitOfMeasure,
    string Status,
    string SourceReference,
    string? LotNumber,
    string? ExpirationDate,
    string SourceType,
    string? SupplierName,
    string? SourceMetadata,
    DateTimeOffset CreatedAt,
    string? Ncm,
    string? Gtin);
