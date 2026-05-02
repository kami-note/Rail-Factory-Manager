using RailFactory.Production.Api.Application;

namespace RailFactory.Production.Api.Api;

public static class ProductionEndpoints
{
    private const string RootPath = "/";
    private const string InfoPath = "/info";

    public static WebApplication MapProductionEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        return app;
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetProductionInfo getProductionInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getProductionInfo.Execute(
            environment.EnvironmentName,
            tenant?.Code,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }
}
