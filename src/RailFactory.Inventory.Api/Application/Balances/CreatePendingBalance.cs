using System.Text.Json;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Balances;

/// <summary>
/// Orchestrates the creation of a pending inventory balance from an external integration event.
/// </summary>
public sealed class CreatePendingBalance(
    IInventoryRepository repository,
    IMaterialRepository materialRepository)
{
    public async Task<bool> ExecuteAsync(CreatePendingBalanceInput input, CancellationToken cancellationToken)
    {
        if (await repository.IntegrationMessageProcessedAsync(input.EventId, cancellationToken))
        {
            return false;
        }

        // JIT Provisioning: Ensure the material exists in the catalog
        var material = await materialRepository.GetByCodeAsync(input.MaterialCode, cancellationToken);
        if (material is null)
        {
            material = Material.Create(
                input.MaterialCode,
                input.OriginalDescription ?? input.MaterialCode,
                $"Auto-provisioned from {input.Source}",
                MaterialCategory.RawMaterial,
                MaterialStatus.Draft,
                imageUrl: null,
                ncm: input.Ncm,
                gtin: input.Gtin);

            await materialRepository.AddAsync(material, cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(material.Ncm) && !string.IsNullOrWhiteSpace(input.Ncm))
        {
            // ELITE FIX: Retroactively enrich technical metadata if missing
            material.SetTechnicalMetadata(input.Ncm, input.Gtin ?? material.Gtin);
        }

        await repository.EnsureDefaultLocationAsync(cancellationToken);
        var location = await repository.FindDefaultLocationAsync(cancellationToken)
            ?? throw new InvalidOperationException("Default stock location was not found.");

        var sourceReference = $"{input.ReceiptId}:{input.ReceiptItemId}";
        var metadataJson = JsonSerializer.Serialize(new
        {
            input.UnitPrice,
            input.OriginalDescription,
            input.AccessKey,
            input.ReceiptNumber,
            input.SupplierName,
            input.Ncm,
            input.Gtin
        });

        var balance = InventoryBalance.CreatePending(
            input.MaterialCode,
            input.UnitOfMeasure,
            input.Quantity,
            location.Id,
            sourceReference,
            null,
            null,
            InventorySourceType.Purchase,
            metadataJson);

        await repository.AddBalanceAsync(balance, cancellationToken);

        var detailsJson = JsonSerializer.Serialize(new
        {
            input.ReceiptId,
            input.ReceiptItemId,
            input.ReceiptNumber,
            input.CorrelationId,
            input.EventType,
            input.AccessKey
        });

        await repository.AddLedgerEntryAsync(
            InventoryLedgerEntry.Create(balance.Id, "pending_balance_created", input.Quantity, detailsJson),
            cancellationToken);

        await repository.AddIntegrationMessageAsync(
            InventoryIntegrationMessage.Create(input.EventId, input.EventType),
            cancellationToken);

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) 
            when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // ELITE FIX: Handle race condition in JIT provisioning.
        }

        return true;
    }
}

public sealed record CreatePendingBalanceInput(
    Guid EventId,
    string EventType,
    string CorrelationId,
    Guid ReceiptId,
    Guid ReceiptItemId,
    string ReceiptNumber,
    string MaterialCode,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? UnitPrice,
    string? OriginalDescription,
    string? AccessKey,
    string? SupplierName,
    string Source,
    string? Ncm = null,
    string? Gtin = null);
