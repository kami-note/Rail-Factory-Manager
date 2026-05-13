using RailFactory.BuildingBlocks.Tenancy;

namespace RailFactory.Iam.Api.Infrastructure.Auth.Persistence;

public sealed class IamTenantRoleRecord
{
    public Guid Id { get; init; }
    public required string TenantCode { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    /// <summary>
    /// Collection of atomic permissions assigned to this role.
    /// Serialized as a JSON array of strings in the database.
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    /// <summary>
    /// Collection of role IDs that this role inherits from (Composite Role pattern).
    /// Serialized as a JSON array of GUIDs in the database.
    /// </summary>
    public List<Guid> ChildRoleIds { get; set; } = [];

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
