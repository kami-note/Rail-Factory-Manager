using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Retrieves full details of a material receipt, including supplier info, audit trail, and items.
/// </summary>
public sealed class GetMaterialReceiptDetails(ISupplyChainRepository repository)
{
    /// <summary>
    /// Executes the retrieval of material receipt details.
    /// </summary>
    /// <param name="receiptId">The unique identifier of the receipt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A detailed response or null if not found.</returns>
    public async Task<MaterialReceiptDetailsResponse?> ExecuteAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await repository.GetReceiptByIdAsync(receiptId, cancellationToken);
        if (receipt is null) return null;

        var supplier = await repository.GetSupplierByIdAsync(receipt.SupplierId, cancellationToken);
        var auditEntries = await repository.GetAuditEntriesByReceiptIdAsync(receiptId, cancellationToken);

        var firstEntry = auditEntries.FirstOrDefault(x => x.Action == "receipt_created");
        var startEntry = auditEntries.FirstOrDefault(x => x.Action == "conference_started");

        return new MaterialReceiptDetailsResponse(
            Id: receipt.Id,
            ReceiptNumber: receipt.ReceiptNumber,
            Status: receipt.Status.ToString(),
            Supplier: supplier is null ? null : new MaterialReceiptSupplierResponse(
                Id: supplier.Id,
                Name: supplier.Name,
                TaxId: supplier.FiscalId),
            IssuedAt: receipt.ReceiptDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Audit: new MaterialReceiptAuditResponse(
                CreatedAt: receipt.CreatedAt,
                CreatedBy: firstEntry?.UserIdentifier ?? "System",
                ConferenceStartedAt: startEntry?.CreatedAt,
                ConferenceStartedBy: startEntry?.UserIdentifier),
            CanStartConference: receipt.Status == MaterialReceiptStatus.Registered,
            Items: receipt.Items.Select(i => new MaterialReceiptItemResponse(
                Id: i.Id,
                MaterialCode: i.MaterialCode,
                ProductName: i.OriginalDescription ?? i.MaterialCode,
                OriginalDescription: i.OriginalDescription,
                ExpectedQuantity: i.ExpectedQuantity,
                CountedQuantity: i.CountedQuantity,
                UnitOfMeasure: i.UnitOfMeasure,
                UnitPrice: i.UnitPrice,
                LotNumber: i.ConfirmedLotNumber,
                ExpirationDate: i.ConfirmedExpirationDate?.ToString("yyyy-MM-dd")
            )).ToList(),
            Timeline: auditEntries.Select(a => new MaterialReceiptTimelineResponse(
                Status: MapActionToStatus(a.Action),
                OccurredAt: a.CreatedAt
            )).ToList()
        );
    }

    private static string MapActionToStatus(string action) => action switch
    {
        "receipt_created" => "Registered",
        "conference_started" => "InConference",
        "conference_closed" => "Conferred",
        _ => action
    };
}

/// <summary>
/// Detailed response for a material receipt.
/// </summary>
public record MaterialReceiptDetailsResponse(
    Guid Id,
    string ReceiptNumber,
    string Status,
    MaterialReceiptSupplierResponse? Supplier,
    DateTime IssuedAt,
    MaterialReceiptAuditResponse Audit,
    bool CanStartConference,
    List<MaterialReceiptItemResponse> Items,
    List<MaterialReceiptTimelineResponse> Timeline);

/// <summary>
/// Supplier information for the receipt.
/// </summary>
public record MaterialReceiptSupplierResponse(Guid Id, string Name, string TaxId);

/// <summary>
/// Audit information for the receipt.
/// </summary>
public record MaterialReceiptAuditResponse(DateTimeOffset CreatedAt, string CreatedBy, DateTimeOffset? ConferenceStartedAt, string? ConferenceStartedBy);

/// <summary>
/// Item details within the receipt.
/// </summary>
public record MaterialReceiptItemResponse(
    Guid Id,
    string MaterialCode,
    string ProductName,
    string? OriginalDescription,
    decimal ExpectedQuantity,
    decimal? CountedQuantity,
    string UnitOfMeasure,
    decimal? UnitPrice,
    string? LotNumber,
    string? ExpirationDate);

/// <summary>
/// Timeline event for the receipt.
/// </summary>
public record MaterialReceiptTimelineResponse(string Status, DateTimeOffset OccurredAt);
