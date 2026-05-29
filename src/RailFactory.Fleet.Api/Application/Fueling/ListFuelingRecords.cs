using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Fueling;

public sealed class ListFuelingRecords(IFuelingRepository fueling)
{
    public Task<List<FuelingRecord>> ExecuteAsync(Guid vehicleId, CancellationToken ct)
        => fueling.ListByVehicleAsync(vehicleId, ct);
}
