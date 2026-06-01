using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Skills;

public sealed class ListPersonSkills(IPersonSkillRepository skillRepo)
{
    public Task<List<PersonSkill>> ExecuteAsync(Guid personId, CancellationToken ct)
        => skillRepo.ListByPersonIdAsync(personId, ct);
}
