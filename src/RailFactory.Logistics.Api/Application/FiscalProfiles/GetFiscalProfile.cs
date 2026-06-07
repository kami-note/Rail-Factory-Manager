using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.FiscalProfiles;

public sealed class GetFiscalProfile(IFiscalProfileRepository repository)
{
    public Task<TenantFiscalProfile?> ExecuteAsync(CancellationToken ct)
        => repository.GetAsync(ct);
}
