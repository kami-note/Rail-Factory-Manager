using Microsoft.AspNetCore.Authentication.Google;
using RailFactory.Iam.Api.Application.Auth;

namespace RailFactory.Iam.Api.Infrastructure.Auth;

internal sealed class GoogleExternalIdentityProvider : IExternalIdentityProvider
{
    private readonly GoogleOAuthRedirects redirects;
    private readonly ITenantCatalogClient tenantCatalogClient;

    public GoogleExternalIdentityProvider(GoogleOAuthRedirects redirects, ITenantCatalogClient tenantCatalogClient)
    {
        this.redirects = redirects;
        this.tenantCatalogClient = tenantCatalogClient;
    }

    public async Task<ExternalLoginStartResult> StartGoogleLoginAsync(string tenantCode, string? returnUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return ExternalLoginStartResult.Fail(StatusCodes.Status400BadRequest, TenantConstants.CodeRequiredErrorCode, "Tenant code is required.");
        }

        var resolvedTenant = await tenantCatalogClient.ResolveAsync(tenantCode.Trim(), cancellationToken);
        if (!resolvedTenant.Found)
        {
            return ExternalLoginStartResult.Fail(StatusCodes.Status404NotFound, TenantConstants.NotFoundErrorCode, "Tenant was not found.");
        }

        if (!resolvedTenant.IsActive)
        {
            return ExternalLoginStartResult.Fail(StatusCodes.Status400BadRequest, TenantConstants.InactiveErrorCode, "Tenant is inactive.");
        }

        var redirectUri = redirects.BuildFinalizeRedirectPath(resolvedTenant.Code, returnUrl ?? "/");
        return ExternalLoginStartResult.Challenge(GoogleDefaults.AuthenticationScheme, redirectUri);
    }
}
