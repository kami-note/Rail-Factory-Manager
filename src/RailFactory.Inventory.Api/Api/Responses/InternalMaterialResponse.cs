namespace RailFactory.Inventory.Api.Api.Responses;

public sealed record InternalMaterialResponse(
    string MaterialCode,
    string OfficialName,
    string UnitOfMeasure,
    string? ImageUrl);
