using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient("gateway", client =>
{
    client.BaseAddress = new Uri("http://gateway");
});

var app = builder.Build();

app.UseServiceDefaults();
app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Redirect("/api/status"));
app.MapGet("/api/status", async (HttpContext httpContext, IHttpClientFactory httpClientFactory, IHostEnvironment environment, CancellationToken cancellationToken) =>
{
    var tenantCode = httpContext.ReadTenantCodeHeader();
    if (string.IsNullOrWhiteSpace(tenantCode))
    {
        return TenantHttpResults.CodeRequired();
    }

    var gateway = httpClientFactory.CreateClient("gateway");
    gateway.DefaultRequestHeaders.Remove(TenantConstants.TenantCodeHeaderName);
    gateway.DefaultRequestHeaders.TryAddWithoutValidation(TenantConstants.TenantCodeHeaderName, tenantCode);

    object gatewayStatus;
    try
    {
        gatewayStatus = await gateway.GetFromJsonAsync<object>("/info", cancellationToken)
            ?? new { status = "empty" };
    }
    catch (Exception ex)
    {
        gatewayStatus = new { status = "unavailable", error = ex.Message };
    }

    return Results.Ok(new
    {
        service = "frontend-bff",
        environment = environment.EnvironmentName,
        tenant = new
        {
            code = tenantCode
        },
        gateway = gatewayStatus
    });
});

app.Run();
