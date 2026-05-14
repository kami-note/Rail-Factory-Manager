using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.Inventory.Api.Api.Responses;

/// <summary>
/// Detailed response for an inventory balance.
/// </summary>
public record InventoryBalanceDetailsResponse(
    Guid Id,
    string MaterialCode,
    MaterialDetailsResponse Material,
    string UnitOfMeasure,
    DisplayStatus Status,
    DateTimeOffset CreatedAt,
    InventoryBalanceQuantitiesResponse Quantities,
    InventoryBalanceTraceabilityResponse Traceability,
    List<InventoryBalanceLedgerResponse> Ledger);

/// <summary>
/// Structured metadata for the material associated with the balance.
/// </summary>
public record MaterialDetailsResponse(
    string MaterialCode,
    string OfficialName,
    string Description,
    DisplayStatus Category,
    DisplayStatus Status,
    string? ImageUrl,
    string? Ncm,
    string? Gtin);

/// <summary>
/// Quantity breakdown for the balance.
/// </summary>
public record InventoryBalanceQuantitiesResponse(decimal TotalPhysical, decimal Available, decimal Blocked, decimal Quarantine);

/// <summary>
/// Traceability information for the balance.
/// </summary>
public record InventoryBalanceTraceabilityResponse(
    string? LotNumber, 
    string? ExpirationDate, 
    DisplayStatus SourceType, 
    string SourceReference, 
    string? SupplierName);

/// <summary>
/// Ledger entry for the balance history.
/// </summary>
public record InventoryBalanceLedgerResponse(DateTimeOffset OccurredAt, decimal QuantityChange, DisplayStatus NewStatus, string Reason, string User);
