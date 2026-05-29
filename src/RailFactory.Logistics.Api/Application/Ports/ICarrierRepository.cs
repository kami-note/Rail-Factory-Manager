using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Ports;

public interface ICarrierRepository
{
    Task<Carrier?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Carrier>> ListAsync(CarrierStatus? status, CancellationToken ct);
    Task SaveAsync(Carrier carrier, CancellationToken ct);
}
