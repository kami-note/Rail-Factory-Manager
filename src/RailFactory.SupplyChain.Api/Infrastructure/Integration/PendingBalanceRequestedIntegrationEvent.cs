namespace RailFactory.SupplyChain.Api.Infrastructure.Integration;

public sealed record PendingBalanceRequestedIntegrationEvent(
    Guid ReceiptId,
    Guid ReceiptItemId,
    string ReceiptNumber,
    string TenantCode,
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure,
    string Source);
