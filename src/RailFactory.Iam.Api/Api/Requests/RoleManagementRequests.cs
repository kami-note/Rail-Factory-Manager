namespace RailFactory.Iam.Api.Api.Requests;

public sealed record CreateRoleRequest(string Name, string? Description, List<string> Permissions, List<Guid> ChildRoleIds);

public sealed record AssignRoleRequest(Guid RoleId);

public sealed record RoleResponse(Guid Id, string Name, string? Description, List<string> Permissions, List<Guid> ChildRoleIds);
