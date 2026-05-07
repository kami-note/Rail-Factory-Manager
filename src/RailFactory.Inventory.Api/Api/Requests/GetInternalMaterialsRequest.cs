namespace RailFactory.Inventory.Api.Api.Requests;

public sealed record GetInternalMaterialsRequest(IEnumerable<string> MaterialCodes);
