using RailFactory.BuildingBlocks.Results;
using RailFactory.Tenancy.Api.Application;
using RailFactory.Tenancy.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTenancyModule(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Redirect("/info"));
app.MapGet("/info", async (IHostEnvironment environment, GetTenantByCode getTenant, CancellationToken cancellationToken) =>
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
app.MapGet("/tenants/{code}", async (string code, GetTenantByCode getTenant, CancellationToken cancellationToken) =>
{
    var result = await getTenant.ExecuteAsync(code, cancellationToken);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : ToHttpResult(result.Error);
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
