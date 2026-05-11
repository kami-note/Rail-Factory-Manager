using RailFactory.BuildingBlocks.Tenancy;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides access to the current authenticated actor's identity.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the email address of the current user.
    /// Defaults to a system identifier if no user is authenticated.
    /// </summary>
    EmailAddress Email { get; }

    /// <summary>
    /// Explicitly sets the system identity for the current scope (e.g., for background workers).
    /// </summary>
    void SetSystemIdentity(string identityName);
}
