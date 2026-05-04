namespace RailFactory.SupplyChain.Api.Application.Receiving;

public sealed class ReceiptAlreadyExistsException(string receiptNumber)
    : InvalidOperationException($"Receipt number '{receiptNumber}' already exists.")
{
    public string ReceiptNumber { get; } = receiptNumber;
}
