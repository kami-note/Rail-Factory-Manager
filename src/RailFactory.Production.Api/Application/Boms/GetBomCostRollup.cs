using RailFactory.Production.Api.Application.Ports;

namespace RailFactory.Production.Api.Application.Boms;

/// <summary>
/// Use case to estimate the total theoretical material cost for a single unit of a finished product.
/// </summary>
/// <remarks>
/// This usecase retrieves the BOM structure, queries the latest component purchase prices from the Supply Chain API,
/// scales quantities by the BOM batch size, applies the Technical Loss Factor (Scrap Factor), and sums up the total cost.
/// </remarks>
public sealed class GetBomCostRollup(IBomRepository bomRepository, IMaterialCostProvider costProvider)
{
    /// <summary>
    /// Executes the costing rollup for the specified BOM version.
    /// </summary>
    /// <param name="bomId">Unique identifier of the BOM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A detailed rollup result containing itemized and total costs.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the BOM with the specified ID does not exist.</exception>
    public async Task<BomCostRollupResult> ExecuteAsync(Guid bomId, CancellationToken cancellationToken)
    {
        var bom = await bomRepository.GetByIdAsync(bomId, cancellationToken)
            ?? throw new KeyNotFoundException($"BOM '{bomId}' not found.");

        var materialCodes = bom.Items.Select(i => i.MaterialCode.Value).Distinct().ToList();
        var costs = await costProvider.GetMaterialCostsAsync(materialCodes, cancellationToken);

        var items = new List<BomCostRollupItemResult>();
        decimal totalCost = 0m;

        foreach (var item in bom.Items)
        {
            var code = item.MaterialCode.Value;
            var unitPrice = costs.TryGetValue(code, out var price) ? price : 0m;
            
            // Formula: scaled quantity per unit of finished product, adjusted for the technical scrap factor.
            // ScaledQuantity = (Item.Quantity / BOM.BatchSize) * (1 + Item.ScrapFactor)
            var scaledQuantity = (item.Quantity / bom.BatchSize) * (1 + item.ScrapFactor);
            var itemCost = scaledQuantity * unitPrice;

            totalCost += itemCost;

            items.Add(new BomCostRollupItemResult(
                code,
                item.Quantity,
                item.UnitOfMeasure,
                item.ScrapFactor,
                scaledQuantity,
                unitPrice,
                itemCost
            ));
        }

        return new BomCostRollupResult(
            bom.Id,
            bom.ProductCode.Value,
            bom.BatchSize,
            totalCost,
            items
        );
    }
}

/// <summary>
/// Holds the summary data for the costing rollup calculation.
/// </summary>
public sealed record BomCostRollupResult(
    Guid BomId,
    string ProductCode,
    decimal BatchSize,
    decimal TotalEstimatedCost,
    IReadOnlyCollection<BomCostRollupItemResult> Items
);

/// <summary>
/// Holds the costing data for a single component in the rollup.
/// </summary>
public sealed record BomCostRollupItemResult(
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal ScrapFactor,
    decimal ScaledQuantity,
    decimal UnitPrice,
    decimal TotalCost
);
