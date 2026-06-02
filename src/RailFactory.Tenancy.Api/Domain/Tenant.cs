using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Tenancy.Api.Domain;

public sealed class Tenant : AggregateRoot<string>
{
    private readonly Dictionary<string, string> _connectionStrings = new();

    private Tenant(
        string code,
        string displayName,
        string locale,
        string timeZone,
        TenantStatus status,
        IReadOnlyDictionary<string, string>? connectionStrings = null)
        : base(code)
    {
        DisplayName = displayName;
        Locale = locale;
        TimeZone = timeZone;
        Status = status;

        if (connectionStrings != null)
        {
            foreach (var kvp in connectionStrings)
            {
                _connectionStrings[kvp.Key] = kvp.Value;
            }
        }
    }

    public string Code => Id;

    public string DisplayName { get; }

    public string Locale { get; }

    public string TimeZone { get; }

    public TenantStatus Status { get; }

    public IReadOnlyDictionary<string, string> ConnectionStrings => _connectionStrings.AsReadOnly();

    public bool IsActive => Status == TenantStatus.Active;

    public void SetConnectionString(string serviceName, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionStrings[serviceName] = connectionString;
    }

    public static Tenant RegisterDevTenant()
    {
        var tenant = new Tenant(
            "dev",
            "Tenant de desenvolvimento",
            "pt-BR",
            "America/Sao_Paulo",
            TenantStatus.Active);

        tenant.RaiseDomainEvent(new TenantRegisteredDomainEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            tenant.Code));

        return tenant;
    }

    // Standard database names every tenant needs
    private static readonly string[] ServiceDbs =
        ["iamdb", "supplychaindb", "inventorydb", "productiondb", "hrdb", "fleetdb", "logisticsdb"];

    /// <summary>
    /// Registers a new tenant. Connection strings follow the convention tenant-{code}-{dbname}.
    /// </summary>
    public static Tenant Register(string code, string displayName, string locale, string timeZone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);

        var normalizedCode = code.Trim().ToLowerInvariant();
        var tenant = new Tenant(normalizedCode, displayName.Trim(), locale.Trim(), timeZone.Trim(), TenantStatus.Active);
        foreach (var db in ServiceDbs)
            tenant.SetConnectionString(db, $"tenant-{normalizedCode}-{db}");

        tenant.RaiseDomainEvent(new TenantRegisteredDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, tenant.Code));
        return tenant;
    }

    public static Tenant Restore(
        string code,
        string displayName,
        string locale,
        string timeZone,
        TenantStatus status,
        IReadOnlyDictionary<string, string>? connectionStrings = null)
    {
        return new Tenant(code, displayName, locale, timeZone, status, connectionStrings);
    }
}
