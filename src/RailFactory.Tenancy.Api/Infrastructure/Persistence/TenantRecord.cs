namespace RailFactory.Tenancy.Api.Infrastructure.Persistence;

public sealed class TenantRecord
{
    public required string Code { get; init; }
    public required string DisplayName { get; set; }
    public required string Locale { get; set; }
    public required string TimeZone { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
