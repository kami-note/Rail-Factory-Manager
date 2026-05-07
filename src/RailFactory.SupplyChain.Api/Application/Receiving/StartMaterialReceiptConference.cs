using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Orchestrates the transition of a material receipt to the conference phase.
/// </summary>
public sealed class StartMaterialReceiptConference(
    ISupplyChainRepository repository,
    ISupplyChainTransactionRunner transactionRunner)
{
    /// <summary>
    /// Executes the start conference command.
    /// </summary>
    /// <param name="receiptId">The ID of the receipt to start.</param>
    /// <param name="userIdentifier">The user initiating the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated <see cref="MaterialReceipt"/>.</returns>
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
