namespace RailFactory.BuildingBlocks.Auth;

public sealed record AuthSessionDto(bool Authenticated, AuthUserDto? User)
{
    public static AuthSessionDto Unauthenticated { get; } = new(false, null);

    public static AuthSessionDto CreateAuthenticated(string? name, string? email, IEnumerable<string>? permissions = null)
        => new(true, new AuthUserDto(name, email, permissions ?? []));
}

public sealed record AuthUserDto(string? Name, string? Email, IEnumerable<string> Permissions);
