using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.BuildingBlocks.Tenancy;

/// <summary>
/// Represents a normalized and validated business identifier for a material or product.
/// </summary>
/// <remarks>
/// This Value Object ensures that any SKU/MaterialCode is automatically 
/// trimmed and uppercased, preventing identity collisions across the ecosystem.
/// </remarks>
public sealed record MaterialCode : ValueObject
{
    /// <summary>
    /// The underlying raw string value.
    /// </summary>
    public string Value { get; }

    private MaterialCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Material code cannot be empty.", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Creates a new MaterialCode instance with automatic normalization.
    /// </summary>
    public static MaterialCode From(string value) => new(value);

    /// <summary>
    /// Implicit conversion to string for convenience in queries and DTOs.
    /// </summary>
    public static implicit operator string(MaterialCode code) => code.Value;

    /// <summary>
    /// Explicit conversion from string.
    /// </summary>
    public static explicit operator MaterialCode(string value) => From(value);

    public override string ToString() => Value;
}
