using RailFactory.Inventory.Api.Api.Responses;
using RailFactory.Inventory.Api.Application.Ports;

namespace RailFactory.Inventory.Api.Application.Materials;

public sealed record GetMaterialSuggestionsInput(
    string? Gtin,
    string? Ncm,
    string? Description,
    string? SupplierFiscalId,
    string? SupplierProductCode);

/// <summary>
/// Retrieves material suggestions based on heuristic logic.
/// </summary>
public sealed class GetMaterialSuggestions(IMaterialRepository repository)
{
    public async Task<List<MaterialSuggestionResponse>> ExecuteAsync(GetMaterialSuggestionsInput input, CancellationToken cancellationToken)
    {
        var suggestions = await repository.GetSuggestionsAsync(
            input.Gtin,
            input.Ncm,
            input.Description,
            input.SupplierFiscalId,
            input.SupplierProductCode,
            cancellationToken);

        return suggestions
            .Select(s => new MaterialSuggestionResponse(
                MaterialDtoMapper.ToResponse(s.Material),
                s.Rank,
                s.Reason))
            .ToList();
    }
}
