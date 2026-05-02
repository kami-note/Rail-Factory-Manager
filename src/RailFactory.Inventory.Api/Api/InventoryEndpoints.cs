using RailFactory.Inventory.Api.Application;

namespace RailFactory.Inventory.Api.Api;

public static class InventoryEndpoints
{
    private const string RootPath = "/";
    private const string InfoPath = "/info";

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        return app;
    }

    private static IResult HandleGetInfo(HttpContext context, IHostEnvironment environment, GetInventoryInfo getInventoryInfo)
    {
        var tenant = context.GetResolvedTenant();

        var response = getInventoryInfo.Execute(
            environment.EnvironmentName,
            tenant?.Code,
            tenant?.Locale,
            tenant?.TimeZone);

        return Results.Ok(response);
    }
}
