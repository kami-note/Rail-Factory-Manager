namespace RailFactory.BuildingBlocks.Tenancy;

public sealed record TenantContext(
    string TenantCode,
    string Locale,
    string TimeZone);
