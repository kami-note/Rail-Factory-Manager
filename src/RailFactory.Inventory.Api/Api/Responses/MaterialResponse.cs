namespace RailFactory.Inventory.Api.Api.Responses;

public sealed record MaterialResponse(
    string MaterialCode,
    string OfficialName,
    string Description,
    string UnitOfMeasure,
    string ProcurementType,
    string Category,
    string Status,
    string? Gtin,
    string? Ncm,
    string? ImageUrl,
    string CreatedBy,
    string LastModifiedBy,
    string? ReplacedBy,
    IReadOnlyList<MaterialSupplierMappingResponse> SupplierMappings);

public sealed record MaterialSupplierMappingResponse(
    string Id,
    string SupplierName,
    string SupplierCode,
    decimal ConversionFactor,
    decimal LastPrice);
