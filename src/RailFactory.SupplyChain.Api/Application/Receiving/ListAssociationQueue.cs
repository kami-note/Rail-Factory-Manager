using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Lists receipts that still need association work before conference.
/// </summary>
public sealed class ListAssociationQueue(ISupplyChainRepository repository)
{
    public async Task<IReadOnlyList<AssociationQueueReceiptResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var receipts = await repository.ListReceiptsAsync(cancellationToken);
        var result = new List<AssociationQueueReceiptResponse>();

        foreach (var receipt in receipts.Where(RequiresAssociationWork))
        {
            var supplier = await repository.GetSupplierByIdAsync(receipt.SupplierId, cancellationToken);
            var totalItems = receipt.Items.Count;
            var resolvedItems = receipt.Items.Count(IsResolved);
            var blockingItems = totalItems - resolvedItems;

            result.Add(new AssociationQueueReceiptResponse(
                receipt.Id,
                receipt.ReceiptNumber,
                supplier?.Name ?? "Unknown supplier",
                receipt.DocumentNumber,
                receipt.ReceiptDate.ToDateTime(TimeOnly.MinValue),
                receipt.Status.ToString(),
                totalItems,
                resolvedItems,
                blockingItems));
        }

        return result;
    }

    private static bool RequiresAssociationWork(MaterialReceipt receipt) =>
        receipt.Status == MaterialReceiptStatus.PendingAssociation ||
        receipt.Items.Any(x => !IsResolved(x));

    private static bool IsResolved(MaterialReceiptItem item) =>
        item.AssociationStatus is MaterialReceiptItemAssociationStatus.Mapped or MaterialReceiptItemAssociationStatus.CreatedAndMapped;
}

public sealed record AssociationQueueReceiptResponse(
    Guid ReceiptId,
    string ReceiptNumber,
    string SupplierName,
    string DocumentNumber,
    DateTime IssuedAt,
    string Status,
    int TotalItems,
    int ResolvedItems,
    int BlockingItems);
