using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Materials;

public sealed record RegisterSupplierMaterialMappingInput(
    string SupplierFiscalId,
    string SupplierProductCode,
    string MaterialCode);

/// <summary>
/// Handles the supply.supplier_material_mapping_created event to create or update a material hint.
/// </summary>
public sealed class RegisterSupplierMaterialMapping(IMaterialRepository repository)
{
    public async Task<bool> ExecuteAsync(RegisterSupplierMaterialMappingInput input, CancellationToken cancellationToken)
    {
        var supplierFiscalId = FiscalId.From(input.SupplierFiscalId);
        var materialCode = MaterialCode.From(input.MaterialCode);

        var hint = SupplierMaterialHint.Create(supplierFiscalId, input.SupplierProductCode, materialCode);

        await repository.UpsertSupplierMaterialHintAsync(hint, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
