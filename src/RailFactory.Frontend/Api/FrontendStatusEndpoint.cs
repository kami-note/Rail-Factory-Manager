using System.Net.Http.Json;
using RailFactory.Frontend.Infrastructure;

namespace RailFactory.Frontend.Api;

internal static class FrontendStatusEndpoint
{
    public static async Task<IResult> HandleGet(
        HttpContext httpContext,
        IHttpClientFactory httpClientFactory,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var tenantCode = httpContext.ReadTenantCodeHeader();
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            return TenantHttpResults.CodeRequired();
        }

        var gateway = httpClientFactory.CreateClient(FrontendHostingExtensions.GatewayClientName);
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
    }
}
