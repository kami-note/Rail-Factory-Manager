using System.Security.Claims;
using System.Text.Json;
using RailFactory.BuildingBlocks.Auth;
using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using RailFactory.Tenancy.Api.Api;
using RailFactory.Tenancy.Api.Api.Requests;
using RailFactory.Tenancy.Api.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddInternalTokenAuthentication(builder.Configuration);

builder.Services.AddTenancyModule(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultEndpoints();

// Root redirect
app.MapGet("/", () => Results.Redirect("/api/tenancy/info"));

var group = app.MapGroup("/api/tenancy");

group.MapGet("/info", async (IHostEnvironment environment, GetTenantByCode getTenant, CancellationToken cancellationToken) =>
{
    var tenant = await getTenant.ExecuteAsync("dev", cancellationToken);

    return Results.Ok(new
    {
        service = "tenancy",
        environment = environment.EnvironmentName,
        purpose = "Tenant catalog and tenant resolution",
        defaultTenant = tenant.IsSuccess ? tenant.Value : null
    });
});

group.MapGet("/tenants/{code}", async ([AsParameters] GetTenantByCodeRequest request, GetTenantByCode getTenant, CancellationToken cancellationToken) =>
{
    var validation = RequestValidator.Validate(request);
    if (validation is not null)
        return validation;

    var result = await getTenant.ExecuteAsync(request.Code, cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Value) : ToHttpResult(result.Error);
});

group.MapGet("/tenants", async (ListTenants listTenants, CancellationToken cancellationToken) =>
{
    var result = await listTenants.ExecuteAsync(cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Value) : ToHttpResult(result.Error);
});

// ── Internal endpoints (service-to-service, InternalApiKey) ──────────────────

var integrationsGroup = group.MapGroup("/integrations");
integrationsGroup.RequireInternalApiKey();

integrationsGroup.MapPost("/{tenantCode}", async (
    string tenantCode,
    [FromBody] ConfigureIntegrationRequest req,
    ConfigureIntegration configure,
    CancellationToken cancellationToken) =>
{
    var result = await configure.ExecuteAsync(tenantCode, req.Category, req.ProviderType, req.Credentials, cancellationToken);
    return result.IsSuccess
        ? Results.Ok(new { id = result.Value })
        : ToHttpResult(result.Error);
});

integrationsGroup.MapGet("/{tenantCode}", async (
    string tenantCode,
    ListTenantIntegrations list,
    CancellationToken cancellationToken) =>
{
    var items = await list.ExecuteAsync(tenantCode, cancellationToken);
    return Results.Ok(items);
});

integrationsGroup.MapGet("/{tenantCode}/{category}/credentials", async (
    string tenantCode,
    string category,
    GetIntegrationCredentials get,
    CancellationToken cancellationToken) =>
{
    var result = await get.ExecuteAsync(tenantCode, category, cancellationToken);
    if (!result.IsSuccess)
        return ToHttpResult(result.Error);

    using var details = result.Value;
    var json = JsonSerializer.SerializeToUtf8Bytes(new
    {
        providerType = details.ProviderType,
        credentials = details.Credentials.ToStringDictionary()
    });
    return Results.Bytes(json, contentType: "application/json");
});

// ── Admin endpoints (JWT + tenancy.admin permission) ─────────────────────────
// Tenant management

var adminTenantsGroup = group.MapGroup("/admin/tenants")
    .RequireAuthorization()
    .RequirePermission(SystemPermissions.Tenancy.Admin);

adminTenantsGroup.MapGet("/", async (ListTenants list, CancellationToken ct) =>
{
    var result = await list.ExecuteAsync(ct);
    return result.IsSuccess ? Results.Ok(result.Value) : ToHttpResult(result.Error);
});

adminTenantsGroup.MapPost("/", async (
    [FromBody] RegisterTenantRequest req,
    RegisterTenant register,
    CancellationToken ct) =>
{
    var result = await register.ExecuteAsync(
        new RegisterTenantInput(req.Code, req.DisplayName, req.Locale ?? "pt-BR", req.TimeZone ?? "America/Sao_Paulo"), ct);
    return result.IsSuccess
        ? Results.Created($"/api/tenancy/tenants/{result.Value.Code}", result.Value)
        : ToHttpResult(result.Error);
});

// ── Admin integration management ──────────────────────────────────────────────
// Tenant code is always derived from the JWT "tenant" claim — never from the URL.
// This prevents cross-tenant privilege escalation.

var adminGroup = group.MapGroup("/admin/integrations")
    .RequireAuthorization()
    .RequirePermission(SystemPermissions.Tenancy.Admin);

adminGroup.MapGet("/", async (
    ClaimsPrincipal user,
    ListTenantIntegrations list,
    CancellationToken cancellationToken) =>
{
    var tenantCode = user.FindFirstValue(InternalServiceTokenClaimTypes.Tenant);
    if (string.IsNullOrWhiteSpace(tenantCode))
        return Results.Problem("Tenant claim missing from token.", statusCode: StatusCodes.Status403Forbidden);
    var items = await list.ExecuteAsync(tenantCode, cancellationToken);
    return Results.Ok(items);
});

adminGroup.MapPost("/", async (
    ClaimsPrincipal user,
    [FromBody] ConfigureIntegrationRequest req,
    ConfigureIntegration configure,
    CancellationToken cancellationToken) =>
{
    var tenantCode = user.FindFirstValue(InternalServiceTokenClaimTypes.Tenant);
    if (string.IsNullOrWhiteSpace(tenantCode))
        return Results.Problem("Tenant claim missing from token.", statusCode: StatusCodes.Status403Forbidden);
    var result = await configure.ExecuteAsync(tenantCode, req.Category, req.ProviderType, req.Credentials, cancellationToken);
    return result.IsSuccess
        ? Results.Ok(new { id = result.Value })
        : ToHttpResult(result.Error);
});

adminGroup.MapPut("/{category}/enable", async (
    string category,
    ClaimsPrincipal user,
    EnableIntegration enable,
    CancellationToken cancellationToken) =>
{
    var tenantCode = user.FindFirstValue(InternalServiceTokenClaimTypes.Tenant);
    if (string.IsNullOrWhiteSpace(tenantCode))
        return Results.Problem("Tenant claim missing from token.", statusCode: StatusCodes.Status403Forbidden);
    var result = await enable.ExecuteAsync(tenantCode, category, cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Value) : ToHttpResult(result.Error);
});

adminGroup.MapPut("/{category}/disable", async (
    string category,
    ClaimsPrincipal user,
    DisableIntegration disable,
    CancellationToken cancellationToken) =>
{
    var tenantCode = user.FindFirstValue(InternalServiceTokenClaimTypes.Tenant);
    if (string.IsNullOrWhiteSpace(tenantCode))
        return Results.Problem("Tenant claim missing from token.", statusCode: StatusCodes.Status403Forbidden);
    var result = await disable.ExecuteAsync(tenantCode, category, cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Value) : ToHttpResult(result.Error);
});

app.Run();

static IResult ToHttpResult(Error error)
{
    var statusCode = error.Code.EndsWith("not_found", StringComparison.Ordinal)
        ? StatusCodes.Status404NotFound
        : error.Code.EndsWith("already_exists", StringComparison.Ordinal)
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status400BadRequest;

    var title = statusCode switch
    {
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Invalid request"
    };

    return Results.Problem(
        title: title,
        detail: error.Message,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
