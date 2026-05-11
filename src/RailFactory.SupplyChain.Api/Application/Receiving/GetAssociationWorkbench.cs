using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Builds the association workbench read model for resolving supplier SKU to internal material SKU decisions.
/// </summary>
public sealed class GetAssociationWorkbench(ISupplyChainRepository repository)
{
    public async Task<AssociationWorkbenchResponse?> ExecuteAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await repository.GetReceiptByIdAsync(receiptId, cancellationToken);
        if (receipt is null)
        {
            return null;
        }

        var supplier = await repository.GetSupplierByIdAsync(receipt.SupplierId, cancellationToken);
        var blockers = BuildReleaseBlockers(receipt.Items);

        return new AssociationWorkbenchResponse(
            new AssociationWorkbenchReceiptResponse(
                receipt.Id,
                receipt.ReceiptNumber,
                receipt.UpdatedAt,
                supplier?.FiscalId ?? string.Empty,
                supplier?.Name ?? "Unknown supplier",
                receipt.Status.ToString(),
                blockers.Count == 0,
                blockers),
            receipt.Items
                .OrderBy(x => x.Id)
                .Select(ToItemResponse)
                .ToList());
    }

    private static List<string> BuildReleaseBlockers(IEnumerable<MaterialReceiptItem> items)
    {
        var blockingCount = items.Count(x => x.AssociationStatus is not MaterialReceiptItemAssociationStatus.Mapped and not MaterialReceiptItemAssociationStatus.CreatedAndMapped);
        return blockingCount == 0
            ? []
            : [$"{blockingCount} item(s) require association decisions."];
    }

    private static AssociationWorkbenchItemResponse ToItemResponse(MaterialReceiptItem item)
    {
        var inventoryQuantity = item.AssociationConversionFactor.HasValue
            ? item.SupplierQuantity * item.AssociationConversionFactor.Value
            : (decimal?)null;

        return new AssociationWorkbenchItemResponse(
            item.Id,
            item.AssociationUpdatedAt,
            item.AssociationStatus.ToString(),
            item.SupplierProductCode,
            item.OriginalDescription ?? item.SupplierProductCode,
            item.OriginalDescription,
            item.Ncm,
            item.Ean,
            item.SupplierUnitOfMeasure,
            item.SupplierQuantity,
            item.UnitPrice,
            item.InternalMaterialCode?.Value,
            item.InternalMaterialCode?.Value,
            item.AssociationConversionFactor,
            inventoryQuantity,
            item.AssociationReason,
            []);
    }
}

public sealed record AssociationWorkbenchResponse(
    AssociationWorkbenchReceiptResponse Receipt,
    IReadOnlyList<AssociationWorkbenchItemResponse> Items);

public sealed record AssociationWorkbenchReceiptResponse(
    Guid Id,
    string ReceiptNumber,
    DateTimeOffset Version,
    string SupplierFiscalId,
    string SupplierName,
    string Status,
    bool CanReleaseToConference,
    IReadOnlyList<string> ReleaseBlockers);

public sealed record AssociationWorkbenchItemResponse(
    Guid ItemId,
    DateTimeOffset Version,
    string AssociationStatus,
    string SupplierProductCode,
    string Description,
    string? OriginalDescription,
    string? Ncm,
    string? Gtin,
    string SupplierUnit,
    decimal Quantity,
    decimal? UnitPrice,
    string? InternalMaterialCode,
    string? InternalMaterialName,
    decimal? ConversionFactor,
    decimal? InventoryQuantity,
    string? ReviewReason,
    IReadOnlyList<AssociationWorkbenchMaterialSuggestionResponse> Suggestions);

public sealed record AssociationWorkbenchMaterialSuggestionResponse(
    string MaterialCode,
    string OfficialName,
    string StockUnit,
    string Confidence,
    string Reason);
