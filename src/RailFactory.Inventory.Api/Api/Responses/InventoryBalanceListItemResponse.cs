using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.Inventory.Api.Api.Responses;

public sealed record InventoryBalanceListItemResponse(
    Guid Id,
    string MaterialCode,
    string MaterialName,
    decimal Quantity,
    string UnitOfMeasure,
    DisplayStatus Status,
    string SourceReference,
    string? LotNumber,
    string? ExpirationDate,
    DisplayStatus SourceType,
    string? SupplierName,
    DateTimeOffset CreatedAt,
    string? Ncm = null,
    string? Gtin = null,
    string? MaterialImageUrl = null);
