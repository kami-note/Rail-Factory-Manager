namespace RailFactory.Inventory.Api.Api.Responses;

/// <summary>
/// Summary response for a pending inventory balance in a list.
/// </summary>
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
