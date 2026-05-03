namespace RailFactory.SupplyChain.Api.Domain;

public sealed class SupplyAuditEntry
{
    public Guid Id { get; private set; }
    public string TenantCode { get; private set; }
    public string Action { get; private set; }
    public string UserIdentifier { get; private set; }
    public string MetadataJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SupplyAuditEntry()
    {
        TenantCode = string.Empty;
        Action = string.Empty;
        UserIdentifier = string.Empty;
        MetadataJson = "{}";
    }

    private SupplyAuditEntry(Guid id, string tenantCode, string action, string userIdentifier, string metadataJson)
    {
        Id = id;
        TenantCode = tenantCode;
        Action = action;
        UserIdentifier = userIdentifier;
        MetadataJson = metadataJson;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static SupplyAuditEntry Create(string tenantCode, string action, string userIdentifier, string metadataJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(userIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataJson);

        return new SupplyAuditEntry(Guid.NewGuid(), tenantCode.Trim(), action.Trim(), userIdentifier.Trim(), metadataJson);
    }
}
