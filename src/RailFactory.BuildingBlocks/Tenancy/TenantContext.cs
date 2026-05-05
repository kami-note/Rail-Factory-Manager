namespace RailFactory.BuildingBlocks.Tenancy;

public sealed record TenantContext(
    string TenantCode,
    string Locale,
    string TimeZone,
    IReadOnlyDictionary<string, string>? ConnectionStrings = null)
{
    public IReadOnlyDictionary<string, string> ConnectionStrings { get; init; } = ConnectionStrings ?? new Dictionary<string, string>();
}
