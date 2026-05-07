using RailFactory.SupplyChain.Api.Api.Responses;
using RailFactory.SupplyChain.Api.Application.Ports;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Retrieves items for a blind conference, omitting expected quantities.
/// </summary>
public sealed class GetConferenceItems(
    ISupplyChainRepository repository,
    IInventoryMaterialService materialService)
{
    /// <summary>
    /// Executes the retrieval of conference items.
    /// </summary>
    public async Task<List<MaterialReceiptConferenceItemResponse>?> ExecuteAsync(Guid receiptId, CancellationToken cancellationToken)
    {
        var receipt = await repository.GetReceiptByIdAsync(receiptId, cancellationToken);
        if (receipt is null) return null;

        // Fetch material metadata for images and standardized names from Inventory service
        var materialCodes = receipt.Items.Select(x => x.MaterialCode.Value).Distinct();
        var materials = await materialService.GetMaterialsByCodesAsync(materialCodes, cancellationToken);

        return receipt.Items.Select(i => 
        {
            var material = materials.TryGetValue(i.MaterialCode, out var m) ? m : null;
            return new MaterialReceiptConferenceItemResponse(
                Id: i.Id,
                MaterialCode: i.MaterialCode,
                UnitOfMeasure: i.UnitOfMeasure,
                OriginalDescription: material?.OfficialName ?? i.OriginalDescription,
                ImageUrl: material?.ImageUrl
            );
        }).ToList();
    }
}
