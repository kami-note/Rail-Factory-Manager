using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Retrieves items from a receipt that do not have a valid supplier mapping.
/// </summary>
public sealed class GetItemsToAssociate(ISupplyChainRepository repository)
{
    public async Task<List<ItemToAssociateResponse>> ExecuteAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await repository.GetReceiptByIdAsync(receiptId, cancellationToken);
        if (receipt is null) return new();

        var supplier = await repository.GetSupplierByIdAsync(receipt.SupplierId, cancellationToken);
        if (supplier is null) return new();

        var itemsToAssociate = new List<ItemToAssociateResponse>();

        foreach (var item in receipt.Items)
        {
            if (item.AssociationStatus == MaterialReceiptItemAssociationStatus.Pending)
            {
                itemsToAssociate.Add(new ItemToAssociateResponse(
                    item.Id,
                    supplier.FiscalId,
                    item.SupplierProductCode,
                    item.OriginalDescription ?? item.SupplierProductCode,
                    item.UnitOfMeasure,
                    item.ExpectedQuantity,
                    item.UnitPrice ?? 0
                ));
            }
        }

        return itemsToAssociate;
    }
}

public sealed record ItemToAssociateResponse(
    Guid ItemId,
    string SupplierFiscalId,
    string SupplierProductCode,
    string Description,
    string SupplierUnit,
    decimal Quantity,
    decimal UnitPrice);
