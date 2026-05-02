using RailFactory.Iam.Api.Api;
using RailFactory.Iam.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddTenantResolution();
builder.Services.AddIamModule(builder.Configuration);
builder.AddIamHosting();

var app = builder.Build();

app.UseIamHosting();
app.MapIamEndpoints();

app.Run();

public partial class Program;
