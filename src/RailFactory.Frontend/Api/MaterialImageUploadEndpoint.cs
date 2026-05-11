using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

internal static class MaterialImageUploadEndpoint
{
    private const string IamSessionPath = "/api/iam/auth/session";

    public static async Task<IResult> HandlePost(
        [AsParameters] UploadMaterialImageRoute route,
        HttpContext httpContext,
        IImageStorage storage,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // When running behind a public HTTPS tunnel/proxy, honor forwarded scheme before CSRF validation.
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

        if (!await IsAuthenticatedAsync(httpContext, tenantCode, httpClientFactory, cancellationToken))
        {
            return Results.Json(AuthSessionDto.Unauthenticated, statusCode: StatusCodes.Status401Unauthorized);
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"];
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { code = "material.image_file_required", message = "An image file is required." });
        }

        if (file.Length > MaterialImageUploadPolicy.MaxUploadBytes)
        {
            return Results.BadRequest(new { code = "material.image_too_large", message = "Image must be 5MB or smaller." });
        }

        if (!MaterialImageUploadPolicy.TryGetExtension(file.ContentType, out var extension))
        {
            return Results.BadRequest(new { code = "material.image_invalid_type", message = "Allowed types: PNG, JPG, WEBP." });
        }

        var normalizedCode = NormalizeMaterialCode(route.MaterialCode);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return Results.BadRequest(new { code = "material.code_invalid", message = "Material code is invalid." });
        }

        var fileName = $"{normalizedCode}{extension}";
        
        using var stream = file.OpenReadStream();
        var imageUrl = await storage.SaveAsync(tenantCode, fileName, stream, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/inventory/materials/{Uri.EscapeDataString(route.MaterialCode)}/image");
        request.Headers.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);
        request.Content = JsonContent.Create(new { imageUrl });

        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);
        using var response = await gateway.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Results.NotFound(new { code = "material.not_found", message = "Material was not found in catalog." });
        }

        if (!response.IsSuccessStatusCode)
        {
            return Results.Problem(
                title: "Failed to persist material image",
                detail: $"Inventory API returned {(int)response.StatusCode}.",
                statusCode: (int)response.StatusCode,
                extensions: new Dictionary<string, object?> { ["code"] = "material.image_persist_failed" });
        }

        return Results.Ok(new { materialCode = route.MaterialCode, imageUrl });
    }

    private static string NormalizeMaterialCode(string materialCode)
    {
        var trimmed = materialCode.Trim();
        return Regex.Replace(trimmed, "[^A-Za-z0-9_-]", "_");
    }

    private static async Task<bool> IsAuthenticatedAsync(
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
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<AuthSessionDto>(cancellationToken: cancellationToken);
        return payload?.Authenticated == true;
    }
}

internal sealed record UploadMaterialImageRoute(string MaterialCode);
