namespace RailFactory.Logistics.Api.Api.Requests;

public sealed record AddShipmentItemRequest(
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal WeightKg,
    decimal VolumeCbm,
    // Fiscal fields (NF-e) — optional, required for automatic fiscal emission
    string NcmCode = "",
    string CfopCode = "",
    decimal UnitValue = 0,
    decimal TaxBaseIcms = 0,
    decimal IcmsRate = 12);
