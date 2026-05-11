using RailFactory.BuildingBlocks.Domain;
using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.SupplyChain.Api.Domain;

/// <summary>
/// Represents the mapping between a supplier's material code (cProd) and the internal catalog material code.
/// </summary>
/// <remarks>
/// Invariant: Provides the "Barreira de Recebimento", stopping auto-provisioning.
/// If a supplier sells in Boxes (CX) but inventory tracks in Units (UN), the <see cref="ConversionFactor"/>
/// allows multiplying quantities and dividing prices safely.
/// Includes audit fields to track who mapped or corrected the association.
/// </remarks>
public sealed class SupplierMaterialMapping : AggregateRoot<Guid>
{
    /// <summary>
    /// The supplier's fiscal identity (CNPJ).
    /// </summary>
    public FiscalId SupplierFiscalId { get; private set; }

    /// <summary>
    /// The supplier's original product code (cProd from XML).
    /// </summary>
    public string SupplierProductCode { get; private set; }

    /// <summary>
    /// The official internal catalog code.
    /// </summary>
    public MaterialCode InternalMaterialCode { get; private set; }

    /// <summary>
    /// The target unit of measure used in internal inventory (e.g., UN, KG).
    /// </summary>
    public string InternalUnitOfMeasure { get; private set; }

    /// <summary>
    /// The unit of measure used by the supplier (e.g., CX, PCT).
    /// </summary>
    public string SupplierUnit { get; private set; }

    /// <summary>
    /// The multiplier used to convert supplier quantities into internal quantities.
    /// Example: If supplier sells in Box of 10 and internal is Unit, factor is 10.
    /// </summary>
    public decimal ConversionFactor { get; private set; }

    /// <summary>
    /// The identity of the actor who created this mapping.
    /// </summary>
    public EmailAddress CreatedBy { get; private set; }

    /// <summary>
    /// The identity of the actor who last modified this mapping.
    /// </summary>
    public EmailAddress LastModifiedBy { get; private set; }

    private SupplierMaterialMapping() : base(Guid.Empty)
    {
        SupplierFiscalId = default!;
        SupplierProductCode = string.Empty;
        InternalMaterialCode = default!;
        InternalUnitOfMeasure = string.Empty;
        SupplierUnit = string.Empty;
        CreatedBy = default!;
        LastModifiedBy = default!;
    }

    private SupplierMaterialMapping(
        Guid id, 
        FiscalId supplierFiscalId, 
        string supplierProductCode, 
        MaterialCode internalMaterialCode, 
        string internalUnitOfMeasure,
        string supplierUnit, 
        decimal conversionFactor,
        EmailAddress createdBy) : base(id)
    {
        SupplierFiscalId = supplierFiscalId;
        SupplierProductCode = supplierProductCode;
        InternalMaterialCode = internalMaterialCode;
        InternalUnitOfMeasure = internalUnitOfMeasure;
        SupplierUnit = supplierUnit;
        ConversionFactor = conversionFactor;
        CreatedBy = createdBy;
        LastModifiedBy = createdBy;
    }

    /// <summary>
    /// Creates a new mapping between a supplier product and an internal catalog product.
    /// </summary>
    public static SupplierMaterialMapping Create(
        FiscalId supplierFiscalId,
        string supplierProductCode,
        MaterialCode internalMaterialCode,
        string internalUnitOfMeasure,
        string supplierUnit,
        decimal conversionFactor,
        EmailAddress createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierProductCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierUnit);
        ArgumentException.ThrowIfNullOrWhiteSpace(internalUnitOfMeasure);
        
        if (conversionFactor <= 0)
        {
            throw new ArgumentException("Conversion factor must be greater than zero.", nameof(conversionFactor));
        }

        return new SupplierMaterialMapping(
            Guid.NewGuid(),
            supplierFiscalId,
            supplierProductCode.Trim(),
            internalMaterialCode,
            internalUnitOfMeasure.Trim().ToUpperInvariant(),
            supplierUnit.Trim().ToUpperInvariant(),
            conversionFactor,
            createdBy);
    }

    /// <summary>
    /// Corrects an erroneous mapping by changing the internal code, unit and conversion factor.
    /// </summary>
    /// <param name="newInternalCode">The corrected internal material code.</param>
    /// <param name="newInternalUnit">The corrected target unit of measure.</param>
    /// <param name="newConversionFactor">The corrected conversion factor.</param>
    /// <param name="modifiedBy">The actor correcting the mapping.</param>
    public void CorrectMapping(MaterialCode newInternalCode, string newInternalUnit, decimal newConversionFactor, EmailAddress modifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newInternalUnit);
        if (newConversionFactor <= 0)
        {
            throw new ArgumentException("Conversion factor must be greater than zero.", nameof(newConversionFactor));
        }

        InternalMaterialCode = newInternalCode;
        InternalUnitOfMeasure = newInternalUnit.Trim().ToUpperInvariant();
        ConversionFactor = newConversionFactor;
        LastModifiedBy = modifiedBy;
    }
}
