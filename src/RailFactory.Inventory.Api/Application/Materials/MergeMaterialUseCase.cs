using System.Text.Json;
using RailFactory.BuildingBlocks.Events;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Materials;

public sealed class MergeMaterialUseCase(
    IMaterialRepository materialRepository,
    IInventoryRepository inventoryRepository) : IMergeMaterialUseCase
{
    public async Task ExecuteAsync(MergeMaterialCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Fetch both materials
        var materials = await materialRepository.GetByCodesAsync(
            [command.ObsoleteMaterialCode.Value, command.OfficialMaterialCode.Value],
            cancellationToken);

        if (!materials.TryGetValue(command.ObsoleteMaterialCode.Value, out var obsoleteMaterial))
        {
            throw new InvalidOperationException($"Obsolete material '{command.ObsoleteMaterialCode}' not found.");
        }

        if (!materials.TryGetValue(command.OfficialMaterialCode.Value, out var officialMaterial))
        {
            throw new InvalidOperationException($"Official material '{command.OfficialMaterialCode}' not found.");
        }

        // 2. Mark the old one as Obsolete and point to the new one
        obsoleteMaterial.MarkObsolete(officialMaterial.MaterialCode, command.ActorId);

        // 3. Fetch all active balances for the obsolete material
        var balances = await inventoryRepository.GetBalancesByMaterialCodeAsync(command.ObsoleteMaterialCode.Value, cancellationToken);

        foreach (var balance in balances.Where(b => b.Quantity > 0))
        {
            // Stock Out logic (Obsolete)
            var stockOutQuantity = balance.Quantity;
            balance.Confirm(0, balance.LotNumber, balance.ExpirationDate, isApproved: false); // Setting to 0 to zero out old balance

            var stockOutEntry = InventoryLedgerEntry.Create(
                balance.Id,
                "StockOut",
                -stockOutQuantity,
                JsonSerializer.Serialize(new { Reason = "Material Merged - Transferred Out", Actor = command.ActorId.Value }));

            await inventoryRepository.AddLedgerEntryAsync(stockOutEntry, cancellationToken);

            // Stock In logic (Official)
            // Preserving physical traceability (Lot, Expiration, Source)
            var newBalance = InventoryBalance.CreatePending(
                officialMaterial.MaterialCode.Value,
                balance.UnitOfMeasure,
                stockOutQuantity,
                balance.StockLocationId,
                $"MERGE-{command.ObsoleteMaterialCode.Value}-{balance.Id}",
                balance.LotNumber,
                balance.ExpirationDate,
                balance.SourceType,
                balance.SourceMetadata);

            // Immediately approve the new balance
            newBalance.Confirm(stockOutQuantity, balance.LotNumber, balance.ExpirationDate, isApproved: true);
            await inventoryRepository.AddBalanceAsync(newBalance, cancellationToken);

            var stockInEntry = InventoryLedgerEntry.Create(
                newBalance.Id,
                "StockIn",
                stockOutQuantity,
                JsonSerializer.Serialize(new { Reason = "Material Merged - Transferred In", Actor = command.ActorId.Value }));

            await inventoryRepository.AddLedgerEntryAsync(stockInEntry, cancellationToken);
        }

        // 4. Publish Event
        var integrationEvent = new MaterialMergedEvent(
            command.ObsoleteMaterialCode,
            command.OfficialMaterialCode,
            command.ActorId);

        // TODO: Enqueue in Outbox/EventBus when available in Inventory context.
        
        // 5. Persist transaction
        await materialRepository.SaveChangesAsync(cancellationToken);
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
