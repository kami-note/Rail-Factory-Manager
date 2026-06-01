using RailFactory.HumanResources.Api.Application.Ports;

namespace RailFactory.HumanResources.Api.Application.Skills;

public sealed class RemovePersonSkill(IPersonSkillRepository skillRepo)
{
    public async Task<bool> ExecuteAsync(Guid skillId, CancellationToken ct)
    {
        var skill = await skillRepo.GetByIdAsync(skillId, ct);
        if (skill is null) return false;

        await skillRepo.RemoveAsync(skill, ct);
        await skillRepo.SaveChangesAsync(ct);
        return true;
    }
}
