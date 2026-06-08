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
    decimal IcmsRate = 12,
    int IcmsOrigin = 0,
    string IcmsCst = "40",
    string PisCst = "07",
    string CofinsCst = "07",
    decimal IpiRate = 0,
    string IpiCst = "99");
