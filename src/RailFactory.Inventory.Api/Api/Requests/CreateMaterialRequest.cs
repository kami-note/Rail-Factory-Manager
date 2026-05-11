namespace RailFactory.Inventory.Api.Api.Requests;

public sealed record CreateMaterialRequest(
    string MaterialCode,
    string OfficialName,
    string Description,
    string UnitOfMeasure,
    string ProcurementType,
    string Category,
    string? Gtin,
    string? Ncm);
