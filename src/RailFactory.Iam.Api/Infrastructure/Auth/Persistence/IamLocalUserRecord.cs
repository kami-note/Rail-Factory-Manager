namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

public sealed class IamLocalUserRecord
{
    public required string ExternalProvider { get; init; }
    public required string ExternalSubject { get; init; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset FirstLoginAt { get; init; }
    public DateTimeOffset LastLoginAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
