using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Ports;

public interface IFuelingRepository
{
    Task<List<FuelingRecord>> ListByVehicleAsync(Guid vehicleId, CancellationToken ct);
    Task SaveAsync(FuelingRecord record, CancellationToken ct);
}
