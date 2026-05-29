using RailFactory.HumanResources.Api.Api;
using RailFactory.HumanResources.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddHrHosting();
builder.Services.AddHrModule(builder.Configuration);

var app = builder.Build();

app.UseHrHosting();
app.MapHrEndpoints();

app.Run();
