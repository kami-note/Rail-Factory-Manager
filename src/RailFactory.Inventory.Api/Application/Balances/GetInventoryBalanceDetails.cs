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
        if (sourceMetadata.HasValue && sourceMetadata.Value.ValueKind == JsonValueKind.Object)
        {
            if (sourceMetadata.Value.TryGetProperty("SupplierName", out var prop) || 
                sourceMetadata.Value.TryGetProperty("supplierName", out prop))
            {
                supplierName = prop.GetString();
            }
        }

        var materialResponse = material is not null
            ? new MaterialDetailsResponse(
                material.MaterialCode,
                material.OfficialName,
                material.Description,
                material.Category.ToString(),
                material.Status.ToString(),
                material.ImageUrl,
                material.Ncm,
                material.Gtin)
            : new MaterialDetailsResponse(balance.MaterialCode, "Unknown Material", "No catalog information available.", "RawMaterial", "Draft", null, null, null);

        return new InventoryBalanceDetailsResponse(
            Id: balance.Id,
            MaterialCode: balance.MaterialCode,
            Material: materialResponse,
            UnitOfMeasure: balance.UnitOfMeasure,
            Status: balance.Status.ToString(),
            Quantities: new InventoryBalanceQuantitiesResponse(
                TotalPhysical: balance.Quantity,
                Available: balance.Status == InventoryBalanceStatus.Available ? balance.Quantity : 0,
                Blocked: balance.Status == InventoryBalanceStatus.Blocked ? balance.Quantity : 0,
                Quarantine: 0
            ),
            Traceability: new InventoryBalanceTraceabilityResponse(
                LotNumber: balance.LotNumber,
                ExpirationDate: balance.ExpirationDate?.ToString("yyyy-MM-dd"),
                SourceType: balance.SourceType.ToString(),
                SourceReference: balance.SourceReference,
                SupplierName: supplierName
            ),
            Ledger: ledger.Select(l => new InventoryBalanceLedgerResponse(
                OccurredAt: l.CreatedAt,
                QuantityChange: l.QuantityDelta,
                NewStatus: balance.Status.ToString(),
                Reason: l.Operation,
                User: "System"
            )).ToList(),
            Audit: new InventoryBalanceAuditResponse(
                LastBlockedAt: null,
                LastBlockedBy: null,
                ReleasedAt: null,
                ReleasedBy: null
            )
        );
    }
}
