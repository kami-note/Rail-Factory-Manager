namespace RailFactory.Gateway.Api;

public static class GatewayEndpoints
{
    private const string RootPath = "/";
    private const string InfoPath = "/info";
    private static readonly string[] GatewayRoutes = ["tenancy", "iam", "supply-chain", "inventory", "production"];

    public static WebApplication MapGatewayEndpoints(this WebApplication app)
    {
        app.MapGet(RootPath, () => Results.Redirect(InfoPath));
        app.MapGet(InfoPath, HandleGetInfo);
        app.MapReverseProxy();
        return app;
    }

    private static IResult HandleGetInfo()
    {
        return Results.Ok(new
        {
            service = "gateway",
            purpose = "YARP entry point for Rail-Factory Fork APIs",
            tenant = "dev",
            routes = GatewayRoutes
        });
    }
}
