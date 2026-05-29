using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Carriers;

public sealed class ListCarriers(ICarrierRepository carriers)
{
    public Task<List<Carrier>> ExecuteAsync(CarrierStatus? status, CancellationToken ct)
        => carriers.ListAsync(status, ct);
}
