using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Ports;

public interface IDispatchRepository
{
    Task<Dispatch?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Dispatch?> GetByTrackingCodeAsync(string trackingCode, CancellationToken ct);
    Task<List<Dispatch>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task SaveAsync(Dispatch dispatch, CancellationToken ct);
}
