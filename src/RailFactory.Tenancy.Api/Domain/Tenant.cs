using RailFactory.BuildingBlocks.Domain;

namespace RailFactory.Tenancy.Api.Domain;

public sealed class Tenant : AggregateRoot<string>
{
    private Tenant(
        string code,
        string displayName,
        string locale,
        string timeZone,
        TenantStatus status)
        : base(code)
    {
        DisplayName = displayName;
        Locale = locale;
        TimeZone = timeZone;
        Status = status;
    }

    public string Code => Id;

    public string DisplayName { get; }

    public string Locale { get; }

    public string TimeZone { get; }

    public TenantStatus Status { get; }

    public bool IsActive => Status == TenantStatus.Active;

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

    public static Tenant Restore(
        string code,
        string displayName,
        string locale,
        string timeZone,
        TenantStatus status)
    {
        return new Tenant(code, displayName, locale, timeZone, status);
    }
}
