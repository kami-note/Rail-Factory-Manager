namespace RailFactory.SupplyChain.Api.Application.Integration;

/// <summary>
/// Integration event emitted when a material receipt item is registered from a fiscal source.
/// </summary>
public sealed record ReceiptItemRegisteredIntegrationEvent(
    Guid ReceiptId,
    Guid ReceiptItemId,
    string ReceiptNumber,
    string? SupplierName,
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? UnitPrice,
    string? OriginalDescription,
    string? AccessKey,
    string Source,
    string? Ncm,
    string? Gtin);

/// <summary>
/// Integration event emitted when a physical conference for a receipt item is closed.
/// </summary>
public sealed record ReceiptItemConferredIntegrationEvent(
    Guid ReceiptId,
    Guid ReceiptItemId,
    string ReceiptStatus,
    bool IsItemApproved,
    decimal CountedQuantity,
    string? LotNumber,
    DateTimeOffset? ExpirationDate,
    string Source);
