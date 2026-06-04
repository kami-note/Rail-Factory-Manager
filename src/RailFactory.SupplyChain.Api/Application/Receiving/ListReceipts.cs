using RailFactory.SupplyChain.Api.Api.Responses;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Retrieves a list of material receipts for the current tenant.
/// </summary>
public sealed class ListReceipts(ISupplyChainRepository repository)
{
    /// <summary>
    /// Executes the retrieval of the material receipt list.
    /// </summary>
    public async Task<List<MaterialReceiptListItemResponse>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var receipts = await repository.ListReceiptsAsync(cancellationToken);
        var result = new List<MaterialReceiptListItemResponse>();

        foreach (var receipt in receipts)
        {
            var supplier = await repository.GetSupplierByIdAsync(receipt.SupplierId, cancellationToken);
            
            result.Add(new MaterialReceiptListItemResponse(
                Id: receipt.Id,
                ReceiptNumber: receipt.ReceiptNumber,
                DocumentNumber: receipt.DocumentNumber,
                SupplierName: supplier?.Name ?? "Unknown Supplier",
                IssuedAt: receipt.ReceiptDate.ToDateTime(TimeOnly.MinValue),
                AccessKey: receipt.AccessKey,
                TotalValue: receipt.TotalValue,
                Status: receipt.Status.ToDisplayStatus(),
                ItemCount: receipt.Items.Count
            ));
        }

        return result;
    }
}

