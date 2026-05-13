using RailFactory.Production.Api.Application;

namespace RailFactory.Production.Api.Api;

public static class ProductionEndpoints
{
    private const string ApiGroup = "/api/production";
    private const string InfoPath = "/info";

    public static WebApplication MapProductionEndpoints(this WebApplication app)
    {
        // Root redirect
        app.MapGet("/", () => Results.Redirect($"{ApiGroup}{InfoPath}"));

        var group = app.MapGroup(ApiGroup);

        group.MapGet(InfoPath, HandleGetInfo).AllowAnonymous();
        
        return app;
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetProductionInfo getProductionInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getProductionInfo.Execute(
            environment.EnvironmentName,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }
}
