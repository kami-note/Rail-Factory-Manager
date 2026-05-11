using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.BuildingBlocks.Events;

/// <summary>
/// Domain Event triggered when two duplicate materials are merged into a single official material.
/// </summary>
/// <remarks>
/// This event is critical for the "Efeito Cascata Seguro". 
/// Supply Chain listens to this to update inflight receipts (NF-es in PendingAssociation).
/// Production listens to this to version BOMs (inactivating BOM v1 with OldCode and creating BOM v2 with NewCode).
/// </remarks>
public sealed record MaterialMergedEvent(
    MaterialCode OldMaterialCode,
    MaterialCode NewMaterialCode,
    EmailAddress ActorId
    );
