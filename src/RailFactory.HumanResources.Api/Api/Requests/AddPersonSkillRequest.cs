namespace RailFactory.HumanResources.Api.Api.Requests;

public sealed record AddPersonSkillRequest(
    string SkillName,
    int ProficiencyLevel,
    DateOnly? CertifiedAt,
    string? Notes);
