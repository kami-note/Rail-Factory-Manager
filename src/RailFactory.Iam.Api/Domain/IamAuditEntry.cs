namespace RailFactory.Iam.Api.Domain;

public sealed class IamAuditEntry
{
    public Guid Id { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ActorEmail { get; private set; } = string.Empty;
    public string? AffectedEmail { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CorrelationId { get; private set; }
    public string MetadataJson { get; private set; } = "{}";
    public DateTimeOffset OccurredAt { get; private set; }

    private IamAuditEntry() { }

    public static IamAuditEntry Create(
        string action,
        string actorEmail,
        string? affectedEmail,
        string? ipAddress,
        string? correlationId,
        string metadataJson = "{}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorEmail);
        return new IamAuditEntry
        {
            Id = Guid.NewGuid(),
            Action = action.Trim(),
            ActorEmail = actorEmail.Trim(),
            AffectedEmail = affectedEmail?.Trim(),
            IpAddress = ipAddress?.Trim(),
            CorrelationId = correlationId?.Trim(),
            MetadataJson = metadataJson,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}
