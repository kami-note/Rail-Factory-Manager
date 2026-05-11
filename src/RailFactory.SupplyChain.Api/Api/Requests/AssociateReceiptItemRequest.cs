namespace RailFactory.SupplyChain.Api.Api.Requests;

public sealed record AssociateReceiptItemRequest(
    DateTimeOffset ExpectedVersion,
    string InternalMaterialCode,
    decimal ConversionFactor);
