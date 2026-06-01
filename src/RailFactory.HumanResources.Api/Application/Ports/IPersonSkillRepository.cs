using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Ports;

public interface IPersonSkillRepository
{
    Task<List<PersonSkill>> ListByPersonIdAsync(Guid personId, CancellationToken ct);
    Task<PersonSkill?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(PersonSkill skill, CancellationToken ct);
    Task RemoveAsync(PersonSkill skill, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
