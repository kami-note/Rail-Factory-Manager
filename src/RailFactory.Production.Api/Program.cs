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
    service = "production",
    environment = environment.EnvironmentName,
    purpose = "Production order placeholder",
    tenant = context.GetResolvedTenant()
}));

app.Run();
