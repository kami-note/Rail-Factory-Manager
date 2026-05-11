using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Transition a material receipt to the conference stage.
/// </summary>
public sealed class StartMaterialReceiptConference(
    ISupplyChainRepository repository,
    ISupplyChainTransactionRunner transactionRunner)
{
    /// <summary>
    /// Starts the conference for the specified receipt.
    /// </summary>
    /// <param name="receiptId">Unique identifier for the receipt.</param>
    /// <param name="userIdentifier">Identity of the actor initiating the conference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated material receipt.</returns>
    /// <exception cref="InvalidOperationException">Thrown if receipt not found or invalid status.</exception>
    public async Task<MaterialReceipt> ExecuteAsync(
        Guid receiptId,
        string userIdentifier,
        CancellationToken cancellationToken)
    {
        MaterialReceipt? receipt = null;

        await transactionRunner.ExecuteAsync(async ct =>
        {
            receipt = await repository.GetReceiptByIdAsync(receiptId, ct)
                ?? throw new InvalidOperationException($"Receipt with ID '{receiptId}' not found.");

            // ELITE FIX: Hardened guard to ensure only fully associated receipts can be conferred.
            if (receipt.Status != MaterialReceiptStatus.Registered)
            {
                throw new InvalidOperationException($"Cannot start conference for receipt in status '{receipt.Status}'. Materials must be associated/created before conference.");
            }

            receipt.StartConference();

            await repository.AddAuditEntryAsync(
                SupplyAuditEntry.Create(
                    "conference_started",
                    userIdentifier,
                    $"{{\"receiptId\": \"{receipt.Id}\", \"receiptNumber\": \"{receipt.ReceiptNumber}\"}}"),
                ct);

            await repository.SaveChangesAsync(ct);
        }, cancellationToken);

        return receipt!;
    }
}
