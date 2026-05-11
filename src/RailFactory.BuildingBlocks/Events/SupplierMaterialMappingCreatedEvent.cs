using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.BuildingBlocks.Events;

/// <summary>
/// Integration event emitted when a supplier product code is mapped to an internal material code.
/// </summary>
/// <remarks>
/// Used to replicate mapping hints to other domains (e.g., Inventory) to avoid cross-domain queries.
/// </remarks>
public sealed record SupplierMaterialMappingCreatedEvent(
    FiscalId SupplierFiscalId,
    string SupplierProductCode,
    MaterialCode MaterialCode
    );
