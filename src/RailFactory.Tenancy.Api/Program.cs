using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using RailFactory.Tenancy.Api.Api;
using RailFactory.Tenancy.Api.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTenancyModule(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
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
    {
        return validation;
    }

    var result = await getTenant.ExecuteAsync(request.Code, cancellationToken);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : ToHttpResult(result.Error);
});

group.MapGet("/tenants", async (ListTenants listTenants, CancellationToken cancellationToken) =>
{
    var result = await listTenants.ExecuteAsync(cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Value) : ToHttpResult(result.Error);
});

app.Run();

static IResult ToHttpResult(Error error)
{
    var statusCode = error.Code.EndsWith("not_found", StringComparison.Ordinal)
        ? StatusCodes.Status404NotFound
        : StatusCodes.Status400BadRequest;

    return Results.Problem(
        title: statusCode == StatusCodes.Status404NotFound ? "Resource not found" : "Invalid request",
        detail: error.Message,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = error.Code
        });
}
