using RailFactory.SupplyChain.Api.Api.Responses;
using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Retrieves items for a blind conference, omitting expected quantities.
/// </summary>
public sealed class GetConferenceItems(ISupplyChainRepository repository)
{
    /// <summary>
    /// Executes the retrieval of conference items.
    /// </summary>
    public async Task<List<MaterialReceiptConferenceItemResponse>?> ExecuteAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await repository.GetReceiptByIdAsync(receiptId, cancellationToken);
        if (receipt is null) return null;

        return receipt.Items.Select(i => new MaterialReceiptConferenceItemResponse(
            Id: i.Id,
            MaterialCode: i.MaterialCode,
            UnitOfMeasure: i.UnitOfMeasure,
            OriginalDescription: i.OriginalDescription
        )).ToList();
    }
}
