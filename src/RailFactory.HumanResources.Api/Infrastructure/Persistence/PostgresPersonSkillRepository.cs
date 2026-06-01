using Microsoft.EntityFrameworkCore;
using RailFactory.HumanResources.Api.Application.Ports;
using RailFactory.HumanResources.Api.Domain;

namespace RailFactory.HumanResources.Api.Infrastructure.Persistence;

public sealed class PostgresPersonSkillRepository(HrDbContext dbContext) : IPersonSkillRepository
{
    public Task<List<PersonSkill>> ListByPersonIdAsync(Guid personId, CancellationToken ct)
        => dbContext.PersonSkills
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.SkillName)
            .ToListAsync(ct);

    public Task<PersonSkill?> GetByIdAsync(Guid id, CancellationToken ct)
        => dbContext.PersonSkills.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task AddAsync(PersonSkill skill, CancellationToken ct)
        => dbContext.PersonSkills.AddAsync(skill, ct).AsTask();

    public Task RemoveAsync(PersonSkill skill, CancellationToken ct)
    {
        dbContext.PersonSkills.Remove(skill);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => dbContext.SaveChangesAsync(ct);
}
