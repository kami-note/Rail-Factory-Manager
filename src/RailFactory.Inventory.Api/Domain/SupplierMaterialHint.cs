using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Inventory.Api.Domain;

/// <summary>
/// A read-model representation of a supplier material mapping.
/// Used to provide highly accurate suggestions when receiving materials.
/// </summary>
public sealed class SupplierMaterialHint
{
    private SupplierMaterialHint() { } // EF Core

    /// <summary>
    /// Creates a new hint from a cross-domain integration event.
    /// </summary>
    public static SupplierMaterialHint Create(
        FiscalId supplierFiscalId,
        string supplierProductCode,
        MaterialCode mappedMaterialCode)
    {
        return new SupplierMaterialHint
        {
            Id = Guid.NewGuid(),
            SupplierFiscalId = supplierFiscalId,
            SupplierProductCode = supplierProductCode,
            MappedMaterialCode = mappedMaterialCode,
            LastSeenAt = DateTimeOffset.UtcNow
        };
    }

    public Guid Id { get; private set; }
    public FiscalId SupplierFiscalId { get; private set; } = null!;
    public string SupplierProductCode { get; private set; } = null!;
    public MaterialCode MappedMaterialCode { get; private set; } = null!;
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>
    /// Updates the hint if the supplier changes the mapping in their system.
    /// </summary>
    public void UpdateMapping(MaterialCode newMaterialCode)
    {
        MappedMaterialCode = newMaterialCode;
        LastSeenAt = DateTimeOffset.UtcNow;
    }
}
