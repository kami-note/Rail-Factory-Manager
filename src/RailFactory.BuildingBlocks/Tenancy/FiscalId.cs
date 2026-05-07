using System.Text.RegularExpressions;
using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.BuildingBlocks.Tenancy;

/// <summary>
/// Represents a normalized and validated tax identifier (e.g., CNPJ or CPF in Brazil).
/// </summary>
/// <remarks>
/// This Value Object ensures that tax identifiers are stored only as digits,
/// preventing identity collisions due to formatting (dots, slashes, dashes).
/// </remarks>
public sealed record FiscalId : ValueObject
{
    /// <summary>
    /// The normalized numeric-only value.
    /// </summary>
    public string Value { get; }

    private FiscalId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Fiscal identifier cannot be empty.", nameof(value));
        }

        // Keep only digits
        var normalized = Regex.Replace(value, @"[^\d]", "");

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Fiscal identifier must contain at least one digit.", nameof(value));
        }

        Value = normalized;
    }

    /// <summary>
    /// Creates a new FiscalId instance with automatic normalization.
    /// </summary>
    public static FiscalId From(string value) => new(value);

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(FiscalId fiscalId) => fiscalId.Value;

    /// <summary>
    /// Explicit conversion from string.
    /// </summary>
    public static explicit operator FiscalId(string value) => From(value);

    public override string ToString() => Value;
}
