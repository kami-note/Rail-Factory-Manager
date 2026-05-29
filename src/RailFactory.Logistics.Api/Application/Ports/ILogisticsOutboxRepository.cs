using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Ports;

public interface ILogisticsOutboxRepository
{
    Task AddAsync(LogisticsOutboxMessage message, CancellationToken ct);
}
