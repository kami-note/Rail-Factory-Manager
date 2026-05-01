var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddTenantResolution();

var app = builder.Build();

app.UseServiceDefaults();
app.UseTenantResolution();
app.MapDefaultEndpoints();
app.MapGet("/", () => Results.Redirect("/info"));
app.MapGet("/info", (HttpContext context, IHostEnvironment environment) => Results.Ok(new
{
    service = "inventory",
    environment = environment.EnvironmentName,
    purpose = "Inventory balance boundary placeholder",
    tenant = context.GetResolvedTenant()
}));

app.Run();
