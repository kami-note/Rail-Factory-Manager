namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

public sealed class IamTenantUserRoleRecord
{
    public required string TenantCode { get; init; }
    
    // User identifier (Composite Key from IamLocalUserRecord)
    public required string ExternalProvider { get; init; }
    public required string ExternalSubject { get; init; }
    
    public required Guid RoleId { get; init; }
    
    // Navigation property (optional for persistence records, but helpful for EF)
    public IamTenantRoleRecord? Role { get; init; }

    public DateTimeOffset AssignedAt { get; init; }
}
