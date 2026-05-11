using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Inventory.Api.Application.Materials;

/// <summary>
/// Port for the use case that handles unifying a duplicate material into an official material.
/// </summary>
/// <remarks>
/// Ensures the "Ajuste Contábil Seguro" by transferring balances (Stock Out / Stock In) without deleting history.
/// Also triggers the <see cref="RailFactory.BuildingBlocks.Events.MaterialMergedEvent"/> to notify other contexts.
/// </remarks>
public interface IMergeMaterialUseCase
{
    /// <summary>
    /// Merges an obsolete material into a new official material.
    /// </summary>
    /// <param name="command">The payload containing old code, new code, and audit context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(MergeMaterialCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Command payload for unifying materials.
/// </summary>
/// <param name="ObsoleteMaterialCode">The material code that will be deactivated (marked Obsolete).</param>
/// <param name="OfficialMaterialCode">The target official material code that absorbs the stock.</param>
/// <param name="ActorId">The identity of the user performing the merge (Auditing Timeline).</param>
public sealed record MergeMaterialCommand(
    MaterialCode ObsoleteMaterialCode,
    MaterialCode OfficialMaterialCode,
    EmailAddress ActorId);
