using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.BuildingBlocks.Tenancy;

/// <summary>
/// Represents a normalized and validated business identifier for a user or actor.
/// </summary>
/// <remarks>
/// This Value Object ensures that any EmailAddress is automatically 
/// trimmed and lowercased, preventing identity collisions.
/// </remarks>
public sealed record EmailAddress : ValueObject
{
    /// <summary>
    /// The underlying raw string value.
    /// </summary>
    public string Value { get; }

    private EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email address cannot be empty.", nameof(value));
        }

        // Simplistic validation for structural integrity.
        if (!value.Contains('@'))
        {
            throw new ArgumentException("Invalid email format.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Creates a new EmailAddress instance with automatic normalization.
    /// </summary>
    public static EmailAddress From(string value) => new(value);

    /// <summary>
    /// Implicit conversion to string for convenience in queries and DTOs.
    /// </summary>
    public static implicit operator string(EmailAddress email) => email.Value;

    /// <summary>
    /// Explicit conversion from string.
    /// </summary>
    public static explicit operator EmailAddress(string value) => From(value);

    public override string ToString() => Value;
}
