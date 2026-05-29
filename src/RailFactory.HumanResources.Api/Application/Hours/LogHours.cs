using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Hours;

public sealed record LogHoursInput(Guid PersonId, DateOnly Date, decimal HoursWorked, string? Description);

public sealed class LogHours(IPersonRepository personRepository, IHourLogRepository hourLogRepository)
{
    public async Task<HourLog> ExecuteAsync(LogHoursInput input, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetByIdAsync(input.PersonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Person {input.PersonId} not found.");

        if (person.Status == PersonStatus.Inactive)
            throw new InvalidOperationException("Cannot log hours for an inactive person.");

        var log = HourLog.Create(input.PersonId, input.Date, input.HoursWorked, input.Description);
        await hourLogRepository.AddAsync(log, cancellationToken);
        await hourLogRepository.SaveChangesAsync(cancellationToken);
        return log;
    }
}
