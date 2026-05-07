namespace RailFactory.SupplyChain.Api.Api.Responses;

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
