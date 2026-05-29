using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Ports;

public interface IHourLogRepository
{
    Task AddAsync(HourLog hourLog, CancellationToken cancellationToken);
    Task<List<HourLog>> ListByPersonAsync(Guid personId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
