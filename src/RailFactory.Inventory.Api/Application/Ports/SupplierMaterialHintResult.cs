using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Application.Ports;

/// <summary>
/// DTO for returning material suggestion results with an associated confidence rank.
/// </summary>
public sealed record SupplierMaterialHintResult(
    Material Material,
    string Rank,
    string Reason);
