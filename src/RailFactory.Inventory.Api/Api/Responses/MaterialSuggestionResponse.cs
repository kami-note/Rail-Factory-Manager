using RailFactory.Inventory.Api.Api.Responses;

namespace RailFactory.Inventory.Api.Api.Responses;

/// <summary>
/// Response model for material suggestions.
/// </summary>
public sealed record MaterialSuggestionResponse(
    MaterialResponse Material,
    string ConfidenceRank,
    string Reason);
