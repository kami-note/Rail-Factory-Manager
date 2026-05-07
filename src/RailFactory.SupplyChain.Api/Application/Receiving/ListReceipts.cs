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

        return receipts.Select(x => new MaterialReceiptListItemResponse(
            Id: x.Id,
            ReceiptNumber: x.ReceiptNumber,
            DocumentNumber: x.DocumentNumber,
            AccessKey: x.AccessKey,
            TotalValue: x.TotalValue,
            Status: x.Status.ToDisplayStatus(),
            ItemCount: x.Items.Count
        )).ToList();
    }
}

