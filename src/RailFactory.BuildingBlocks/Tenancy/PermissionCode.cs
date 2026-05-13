using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.BuildingBlocks.Tenancy;

/// <summary>
/// Represents an atomic permission identifier in the system.
/// </summary>
/// <remarks>
/// Format: lower.case.dot.separated (e.g., "inventory.material.write").
/// Invariant: Max 50 characters, trimmed, and converted to lowercase.
/// </remarks>
public sealed record PermissionCode : ValueObject
{
    public string Value { get; }

    private PermissionCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Permission code cannot be empty.", nameof(value));
        }

        if (value.Length > 50)
        {
            throw new ArgumentException("Permission code cannot exceed 50 characters.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    public static PermissionCode From(string value) => new(value);

    public static implicit operator string(PermissionCode code) => code.Value;

    public static explicit operator PermissionCode(string value) => From(value);

    public override string ToString() => Value;
}
