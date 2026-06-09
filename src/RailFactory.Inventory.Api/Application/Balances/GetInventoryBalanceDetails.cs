using System.Text.Json;
using RailFactory.Inventory.Api.Api.Responses;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Retrieves full details of an inventory balance, including ledger and traceability.
/// </summary>
public sealed class GetInventoryBalanceDetails(
    IInventoryRepository repository,
    IMaterialRepository materialRepository)
{
    /// <summary>
    /// Executes the retrieval of inventory balance details.
    /// </summary>
    /// <param name="id">The unique identifier of the balance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A detailed response or null if not found.</returns>
    public async Task<InventoryBalanceDetailsResponse?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var balance = await repository.GetBalanceByIdAsync(id, cancellationToken);
        if (balance is null) return null;

        var ledger = await repository.GetLedgerEntriesByBalanceIdAsync(id, cancellationToken);
        var material = await materialRepository.GetByCodeAsync(balance.MaterialCode, cancellationToken);

        var sourceMetadata = string.IsNullOrWhiteSpace(balance.SourceMetadata)
            ? (JsonElement?)null
            : JsonSerializer.Deserialize<JsonElement>(balance.SourceMetadata);

        string? supplierName = null;
        string? originalDescription = null;
        Guid? productionOrderId = null;
        string? productionOrderNumber = null;

        if (sourceMetadata.HasValue && sourceMetadata.Value.ValueKind == JsonValueKind.Object)
        {
            if (sourceMetadata.Value.TryGetProperty("SupplierName", out var prop) || 
                sourceMetadata.Value.TryGetProperty("supplierName", out prop))
            {
                supplierName = prop.GetString();
            }

            if (sourceMetadata.Value.TryGetProperty("OriginalDescription", out var descProp) || 
                sourceMetadata.Value.TryGetProperty("originalDescription", out descProp))
            {
                originalDescription = descProp.GetString();
            }

            if (sourceMetadata.Value.TryGetProperty("ProductionOrderId", out var ordIdProp) ||
                sourceMetadata.Value.TryGetProperty("productionOrderId", out ordIdProp))
            {
                if (ordIdProp.TryGetGuid(out var parsedGuid))
                    productionOrderId = parsedGuid;
            }

            if (sourceMetadata.Value.TryGetProperty("OrderNumber", out var ordNumProp) ||
                sourceMetadata.Value.TryGetProperty("orderNumber", out ordNumProp))
            {
                productionOrderNumber = ordNumProp.GetString();
            }
        }

        var materialResponse = material is not null
            ? new MaterialDetailsResponse(
                material.MaterialCode,
                material.OfficialName,
                material.Description,
                material.Category.ToDisplayStatus(),
                material.Status.ToDisplayStatus(),
                material.ImageUrl,
                material.Ncm,
                material.Gtin)
            : new MaterialDetailsResponse(
                balance.MaterialCode,
                originalDescription ?? balance.MaterialCode,
                "No catalog information available.",
                MaterialCategory.RawMaterial.ToDisplayStatus(),
                MaterialStatus.Draft.ToDisplayStatus(),
                null,
                null,
                null);

        // For FinishedGood balances, production info lives in ledger entries (production_output)
        // since the balance is credited in-place rather than created fresh with SourceMetadata.
        if (productionOrderId is null || productionOrderNumber is null)
        {
            var productionOutputEntry = ledger
                .Where(l => l.Operation == "production_output")
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefault();

            if (productionOutputEntry is not null && !string.IsNullOrWhiteSpace(productionOutputEntry.DetailsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(productionOutputEntry.DetailsJson);
                    var root = doc.RootElement;

                    if (productionOrderId is null &&
                        (root.TryGetProperty("ProductionOrderId", out var pIdProp) ||
                         root.TryGetProperty("productionOrderId", out pIdProp)))
                    {
                        if (pIdProp.TryGetGuid(out var parsedGuid))
                            productionOrderId = parsedGuid;
                    }

                    if (productionOrderNumber is null &&
                        (root.TryGetProperty("OrderNumber", out var pNumProp) ||
                         root.TryGetProperty("orderNumber", out pNumProp)))
                    {
                        productionOrderNumber = pNumProp.GetString();
                    }
                }
                catch { /* Ignore malformed DetailsJson */ }
            }
        }

        return new InventoryBalanceDetailsResponse(
            Id: balance.Id,
            MaterialCode: balance.MaterialCode,
            Material: materialResponse,
            UnitOfMeasure: balance.UnitOfMeasure,
            Status: balance.Status.ToDisplayStatus(),
            CreatedAt: balance.CreatedAt,
            Quantities: new InventoryBalanceQuantitiesResponse(
                TotalPhysical: balance.Quantity,
                Available: balance.Status == InventoryBalanceStatus.Available ? balance.Quantity : 0,
                Blocked: balance.Status == InventoryBalanceStatus.Blocked ? balance.Quantity : 0,
                Quarantine: 0
            ),
            Traceability: new InventoryBalanceTraceabilityResponse(
                LotNumber: balance.LotNumber,
                ExpirationDate: balance.ExpirationDate?.ToString("yyyy-MM-dd"),
                SourceType: balance.SourceType.ToDisplayStatus(),
                SourceReference: balance.SourceReference,
                SupplierName: supplierName,
                ProductionOrderId: productionOrderId,
                ProductionOrderNumber: productionOrderNumber
            ),
            Ledger: ledger.Select(l => new InventoryBalanceLedgerResponse(
                OccurredAt: l.CreatedAt,
                QuantityChange: l.QuantityDelta,
                NewStatus: balance.Status.ToDisplayStatus(),
                Reason: l.Operation,
                User: "System"
            )).ToList()
        );

    }
}
