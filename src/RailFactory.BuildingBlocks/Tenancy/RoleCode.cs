using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.BuildingBlocks.Tenancy;

/// <summary>
/// Represents a unique identifier for a Role within a Tenant.
/// </summary>
/// <remarks>
/// Invariant: Trimmed and converted to lowercase to avoid duplication.
/// </remarks>
public sealed record RoleCode : ValueObject
{
    public string Value { get; }

    private RoleCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Role code cannot be empty.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    public static RoleCode From(string value) => new(value);

    public static implicit operator string(RoleCode code) => code.Value;

    public static explicit operator RoleCode(string value) => From(value);

    public override string ToString() => Value;
}
