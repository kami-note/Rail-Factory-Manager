namespace RailFactory.Tenancy.Api.Api.Requests;

public sealed record RegisterTenantRequest(
    string Code,
    string DisplayName,
    string? Locale,
    string? TimeZone);
