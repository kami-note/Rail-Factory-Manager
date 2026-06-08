using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

/// <summary>
/// Endpoint for uploading person profile images.
/// Validates permissions and persists the image into MinIO storage.
/// </summary>
internal static class PersonImageUploadEndpoint
{
    private const string IamSessionPath = "/api/iam/auth/session";

    public static async Task<IResult> HandlePut(
        [AsParameters] UploadPersonImageRoute route,
        HttpContext httpContext,
        IImageStorage storage,
        InternalAccessTokenIssuer tokenIssuer,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // HTTPS Forwarding Scheme Check
        if (!httpContext.Request.IsHttps
            && string.Equals(httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Request.Scheme = "https";
        }

        if (!httpContext.Request.IsHttps)
        {
            return Results.Json(
                new AuthErrorDto("csrf_https_required", "CSRF token validation requires HTTPS."),
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate CSRF
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Json(new AuthErrorDto("csrf_error", "CSRF token validation failed."), statusCode: StatusCodes.Status403Forbidden);
        }

        var tenantCode = httpContext.ReadTenantCodeHeader();
        if (string.IsNullOrWhiteSpace(tenantCode) || !Regex.IsMatch(tenantCode, "^[A-Za-z0-9_-]+$"))
        {
            return TenantHttpResults.CodeRequired();
        }

        var session = await GetAuthenticatedSessionAsync(httpContext, tenantCode, httpClientFactory, cancellationToken);
        if (session is null)
        {
            return Results.Json(AuthSessionDto.Unauthenticated, statusCode: StatusCodes.Status401Unauthorized);
        }

        // Validate HR write permission
        if (!session.User!.Permissions.Contains(SystemPermissions.Hr.Write, StringComparer.Ordinal))
        {
            return Results.Forbid();
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"];
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { code = "person.image_file_required", message = "An image file is required." });
        }

        if (file.Length > MaterialImageUploadPolicy.MaxUploadBytes)
        {
            return Results.BadRequest(new { code = "person.image_too_large", message = "Image must be 5MB or smaller." });
        }

        if (!MaterialImageUploadPolicy.TryGetExtension(file.ContentType, out var extension))
        {
            return Results.BadRequest(new { code = "person.image_invalid_type", message = "Allowed types: PNG, JPG, WEBP." });
        }

        var fileName = $"person_{route.Id:N}{extension}";
        
        using var stream = file.OpenReadStream();
        var imageUrl = await storage.SaveAsync(tenantCode, fileName, stream, cancellationToken);

        // Notify HR API of the updated profile picture
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/hr/people/{route.Id}/image");
        request.Headers.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenIssuer.Issue(session, tenantCode));
        request.Content = JsonContent.Create(new { imageUrl });

        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);
        using var response = await gateway.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { code = "person.not_found", message = "Person was not found." });
        }

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(
                title: "Failed to persist person image",
                detail: $"HR API returned {(int)response.StatusCode}.",
                statusCode: (int)response.StatusCode,
                extensions: new Dictionary<string, object?> { ["code"] = "person.image_persist_failed" });
        }

        return Results.Ok(new { id = route.Id, imageUrl });
    }

    private static async Task<AuthSessionDto?> GetAuthenticatedSessionAsync(
        HttpContext httpContext,
        string tenantCode,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, IamSessionPath);
        request.Headers.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);
        if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());
        }

        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);
        using var response = await gateway.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<AuthSessionDto>(cancellationToken: cancellationToken);
        return payload?.Authenticated == true ? payload : null;
    }
}

internal sealed record UploadPersonImageRoute(Guid Id);
