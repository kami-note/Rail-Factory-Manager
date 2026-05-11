namespace RailFactory.SupplyChain.Api.Application.Receiving;

/// <summary>
/// Exception thrown when an association business rule is violated.
/// </summary>
public sealed class AssociationValidationException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// Exception thrown when a concurrency conflict is detected during association.
/// </summary>
public sealed class AssociationConflictException(Guid itemId, DateTimeOffset currentVersion)
    : InvalidOperationException("The receipt item was changed by another operation. Reload the workbench before saving.")
{
    public Guid ItemId { get; } = itemId;
    public DateTimeOffset CurrentVersion { get; } = currentVersion;
}

/// <summary>
/// Exception thrown when a downstream service returns a validation error.
/// </summary>
public sealed class RemoteServiceValidationException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// Exception thrown when a downstream service returns a conflict error.
/// </summary>
public sealed class RemoteServiceConflictException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}
