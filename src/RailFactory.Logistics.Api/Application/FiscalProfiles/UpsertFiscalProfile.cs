using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.FiscalProfiles;

public sealed record UpsertFiscalProfileInput(
    string CfopPadraoIntraestadual,
    string CfopPadraoInterestadual,
    string UfOrigem,
    decimal IcmsRate,
    string IcmsCst,
    string PisCst,
    string CofinsCst,
    decimal IpiRate,
    int IcmsOrigin);

public sealed class UpsertFiscalProfile(IFiscalProfileRepository repository)
{
    public async Task<TenantFiscalProfile> ExecuteAsync(UpsertFiscalProfileInput input, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        if (existing is null)
        {
            var created = TenantFiscalProfile.Create(
                input.CfopPadraoIntraestadual, input.CfopPadraoInterestadual, input.UfOrigem,
                input.IcmsRate, input.IcmsCst, input.PisCst, input.CofinsCst,
                input.IpiRate, input.IcmsOrigin);
            await repository.UpsertAsync(created, ct);
            return created;
        }

        existing.Update(
            input.CfopPadraoIntraestadual, input.CfopPadraoInterestadual, input.UfOrigem,
            input.IcmsRate, input.IcmsCst, input.PisCst, input.CofinsCst,
            input.IpiRate, input.IcmsOrigin);
        await repository.UpsertAsync(existing, ct);
        return existing;
    }
}
