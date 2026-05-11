using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.SupplyChain.Api.Application.Ports;
using RailFactory.SupplyChain.Api.Domain;

namespace RailFactory.SupplyChain.Api.Application.Suppliers;

/// <summary>
/// Creates or updates a mapping between a supplier's product code and an internal material code.
/// </summary>
public sealed class CreateSupplierMaterialMapping(
    ISupplyChainRepository repository,
    ISupplyChainTransactionRunner transactionRunner)
{
    /// <summary>
    /// Executes the creation or update of a material mapping.
    /// </summary>
    public async Task ExecuteAsync(
        string supplierFiscalId,
        string supplierProductCode,
        string internalMaterialCode,
        string internalUnitOfMeasure,
        string supplierUnit,
        decimal conversionFactor,
        string createdBy,
        CancellationToken cancellationToken)
    {
        await transactionRunner.ExecuteAsync(async (ct) =>
        {
            var existingMapping = await repository.GetSupplierMaterialMappingAsync(
                supplierFiscalId, 
                supplierProductCode, 
                ct);

            if (existingMapping != null)
            {
                existingMapping.CorrectMapping(
                    MaterialCode.From(internalMaterialCode), 
                    internalUnitOfMeasure,
                    conversionFactor, 
                    EmailAddress.From(createdBy));
            }
            else
            {
                var newMapping = SupplierMaterialMapping.Create(
                    FiscalId.From(supplierFiscalId),
                    supplierProductCode,
                    MaterialCode.From(internalMaterialCode),
                    internalUnitOfMeasure,
                    supplierUnit,
                    conversionFactor,
                    EmailAddress.From(createdBy));

                await repository.AddSupplierMaterialMappingAsync(newMapping, ct);
            }

            await repository.SaveChangesAsync(ct);
        }, cancellationToken);
    }
}
