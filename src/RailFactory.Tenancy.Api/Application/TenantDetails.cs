using RailFactory.BuildingBlocks.Presentation;

namespace RailFactory.Tenancy.Api.Application;

public sealed record TenantDetails(
    string Code,
    string DisplayName,
    string Locale,
    string TimeZone,
    DisplayStatus Status,
    IReadOnlyDictionary<string, string> ConnectionStrings);
