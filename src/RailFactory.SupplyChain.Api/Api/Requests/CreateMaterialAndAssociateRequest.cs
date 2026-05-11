namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed record CreateMaterialAndAssociateRequest(
    DateTimeOffset ExpectedVersion,
    string MaterialCode,
    string OfficialName,
    string Description,
    string UnitOfMeasure,
    string ProcurementType,
    string Category,
    string? Gtin,
    string? Ncm,
    decimal ConversionFactor);
