namespace RailFactory.SupplyChain.Api.Api.Requests;

/// <summary>
/// Request to create or update a material mapping.
/// </summary>
public sealed record CreateSupplierMaterialMappingRequest(
    string SupplierFiscalId,
    string SupplierProductCode,
    string InternalMaterialCode,
    string InternalUnitOfMeasure,
    string SupplierUnit,
    decimal ConversionFactor);
