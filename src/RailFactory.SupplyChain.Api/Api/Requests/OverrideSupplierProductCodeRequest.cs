namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed record OverrideSupplierProductCodeRequest(
    DateTimeOffset ExpectedVersion,
    string CorrectedCode,
    string Reason);
