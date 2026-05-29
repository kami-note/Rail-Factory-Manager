using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Hours;

public sealed class ListHourLogs(IHourLogRepository repository)
{
    public Task<List<HourLog>> ExecuteAsync(Guid personId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
        => repository.ListByPersonAsync(personId, from, to, cancellationToken);
}
