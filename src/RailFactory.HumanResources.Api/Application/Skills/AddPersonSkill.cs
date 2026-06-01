using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Application.Skills;

public sealed class AddPersonSkill(IPersonRepository personRepo, IPersonSkillRepository skillRepo)
{
    public async Task<PersonSkill> ExecuteAsync(AddPersonSkillInput input, CancellationToken ct)
    {
        var person = await personRepo.GetByIdAsync(input.PersonId, ct)
            ?? throw new KeyNotFoundException($"Person {input.PersonId} not found.");

        if (person.Status == PersonStatus.Inactive)
            throw new InvalidOperationException("Cannot add skills to an inactive person.");

        var skill = PersonSkill.Create(input.PersonId, input.SkillName, input.ProficiencyLevel, input.CertifiedAt, input.Notes);
        await skillRepo.AddAsync(skill, ct);
        await skillRepo.SaveChangesAsync(ct);
        return skill;
    }
}

public sealed record AddPersonSkillInput(
    Guid PersonId,
    string SkillName,
    int ProficiencyLevel,
    DateOnly? CertifiedAt,
    string? Notes);
