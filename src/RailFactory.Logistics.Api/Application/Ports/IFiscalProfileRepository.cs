using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Ports;

public interface IFiscalProfileRepository
{
    Task<TenantFiscalProfile?> GetAsync(CancellationToken ct);
    Task UpsertAsync(TenantFiscalProfile profile, CancellationToken ct);
}
