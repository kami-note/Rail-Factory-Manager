namespace RailFactory.SupplyChain.Api.Domain;

public sealed class SupplyAuditEntry
{
    public Guid Id { get; private set; }
    public string Action { get; private set; }
    public string UserIdentifier { get; private set; }
    public string MetadataJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SupplyAuditEntry()
    {
        Action = string.Empty;
        UserIdentifier = string.Empty;
        MetadataJson = "{}";
    }

    private SupplyAuditEntry(Guid id, string action, string userIdentifier, string metadataJson)
    {
        Id = id;
        Action = action;
        UserIdentifier = userIdentifier;
        MetadataJson = metadataJson;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static SupplyAuditEntry Create(string action, string userIdentifier, string metadataJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(userIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataJson);

        return new SupplyAuditEntry(Guid.NewGuid(), action.Trim(), userIdentifier.Trim(), metadataJson);
    }
}
