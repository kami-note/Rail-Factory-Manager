using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Api.Responses;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Materials;

/// <summary>
/// Creates a material catalog entry from operator input.
/// For FinishedGood materials, also seeds an initial zero-quantity balance so the
/// product appears immediately in the inventory stocks list.
/// </summary>
public sealed class CreateMaterial(IMaterialRepository repository, IInventoryRepository inventoryRepository)
{
    public async Task<MaterialResponse> ExecuteAsync(CreateMaterialInput input, string actor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.MaterialCode))
        {
            throw new MaterialValidationException("material.code_required", "Material code is required.");
        }

        if (string.IsNullOrWhiteSpace(input.OfficialName))
        {
            throw new MaterialValidationException("material.name_required", "Official name is required.");
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            throw new MaterialValidationException("material.description_required", "Description is required.");
        }

        if (string.IsNullOrWhiteSpace(input.UnitOfMeasure))
        {
            throw new MaterialValidationException("material.unit_required", "Base unit is required.");
        }

        if (!Enum.TryParse<ProcurementType>(input.ProcurementType, ignoreCase: true, out var procurementType) ||
            !Enum.IsDefined(procurementType))
        {
            throw new MaterialValidationException("material.invalid_procurement_type", "Procurement type is not valid.");
        }

        if (!Enum.TryParse<MaterialCategory>(input.Category, ignoreCase: true, out var category) ||
            !Enum.IsDefined(category))
        {
            throw new MaterialValidationException("material.invalid_category", "Material category is not valid.");
        }

        var existingByCode = await repository.GetByCodeAsync(input.MaterialCode, cancellationToken);
        if (existingByCode is not null)
        {
            throw new MaterialValidationException("material.duplicate_code", "Material code already exists.");
        }

        var normalizedGtin = string.IsNullOrWhiteSpace(input.Gtin) ? null : input.Gtin.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedGtin))
        {
            var existingByGtin = await repository.GetByGtinAsync(normalizedGtin, cancellationToken);
            if (existingByGtin is not null)
            {
                throw new MaterialValidationException("material.duplicate_gtin", "GTIN already belongs to another material.");
            }
        }

        var material = Material.Create(
            input.MaterialCode,
            input.OfficialName,
            input.Description,
            category,
            procurementType,
            EmailAddress.From(actor),
            input.UnitOfMeasure,
            MaterialStatus.Verified,
            ncm: input.Ncm,
            gtin: normalizedGtin);

        await repository.AddAsync(material, cancellationToken);

        if (category == MaterialCategory.FinishedGood)
        {
            await inventoryRepository.EnsureDefaultLocationAsync(cancellationToken);
            var location = await inventoryRepository.FindDefaultLocationAsync(cancellationToken)
                ?? throw new InvalidOperationException("Default stock location was not found.");

            var initialBalance = InventoryBalance.CreateInitialFinishedGood(
                input.MaterialCode,
                input.UnitOfMeasure,
                location.Id);

            await inventoryRepository.AddBalanceAsync(initialBalance, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return MaterialDtoMapper.ToResponse(material);
    }
}

public sealed record CreateMaterialInput(
    string MaterialCode,
    string OfficialName,
    string Description,
    string UnitOfMeasure,
    string ProcurementType,
    string Category,
    string? Gtin,
    string? Ncm);

public sealed class MaterialValidationException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
