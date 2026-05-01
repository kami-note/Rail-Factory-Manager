var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseServiceDefaults();
app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Redirect("/info"));
app.MapGet("/info", () => Results.Ok(new
{
    service = "gateway",
    purpose = "YARP entry point for Rail-Factory Fork APIs",
    tenant = "dev",
    routes = new[] { "tenancy", "iam", "supply-chain", "inventory", "production" }
}));
app.MapReverseProxy();

app.Run();
