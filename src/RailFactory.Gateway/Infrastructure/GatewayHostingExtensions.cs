namespace RailFactory.Gateway.Infrastructure;

public static class GatewayHostingExtensions
{
    public static WebApplicationBuilder AddGatewayHosting(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddTenantResolution(); // ELITE FIX: Enable tenant validation at the entry point
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();
        return builder;
    }

    public static WebApplication UseGatewayHosting(this WebApplication app)
    {
        app.UseServiceDefaults();
        app.UseTenantResolution(); // ELITE FIX: Validate tenant existence before proxying

        // NF-05: Security headers on all proxied responses
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // HSTS: 1 year; includeSubDomains — enable only when TLS is confirmed
            if (ctx.Request.IsHttps)
                ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            await next(ctx);
        });

        app.MapDefaultEndpoints();
        return app;
    }
}
