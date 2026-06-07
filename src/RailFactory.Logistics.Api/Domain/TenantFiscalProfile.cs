namespace RailFactory.Logistics.Api.Domain;

public sealed class TenantFiscalProfile
{
    private TenantFiscalProfile() { }

    public string Id { get; private set; } = "default";
    public string CfopPadraoIntraestadual { get; private set; } = "5102";
    public string CfopPadraoInterestadual { get; private set; } = "6102";
    public string UfOrigem { get; private set; } = string.Empty;
    public decimal IcmsRate { get; private set; } = 12m;
    public string IcmsCst { get; private set; } = "40";
    public string PisCst { get; private set; } = "07";
    public string CofinsCst { get; private set; } = "07";
    public decimal IpiRate { get; private set; } = 0m;
    public int IcmsOrigin { get; private set; } = 0;
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TenantFiscalProfile Create(
        string cfopIntra, string cfopInter, string ufOrigem,
        decimal icmsRate, string icmsCst, string pisCst, string cofinsCst,
        decimal ipiRate, int icmsOrigin) => new()
    {
        Id = "default",
        CfopPadraoIntraestadual = cfopIntra,
        CfopPadraoInterestadual = cfopInter,
        UfOrigem = ufOrigem,
        IcmsRate = icmsRate,
        IcmsCst = icmsCst,
        PisCst = pisCst,
        CofinsCst = cofinsCst,
        IpiRate = ipiRate,
        IcmsOrigin = icmsOrigin,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    public void Update(
        string cfopIntra, string cfopInter, string ufOrigem,
        decimal icmsRate, string icmsCst, string pisCst, string cofinsCst,
        decimal ipiRate, int icmsOrigin)
    {
        CfopPadraoIntraestadual = cfopIntra;
        CfopPadraoInterestadual = cfopInter;
        UfOrigem = ufOrigem;
        IcmsRate = icmsRate;
        IcmsCst = icmsCst;
        PisCst = pisCst;
        CofinsCst = cofinsCst;
        IpiRate = ipiRate;
        IcmsOrigin = icmsOrigin;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
