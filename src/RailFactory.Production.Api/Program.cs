using RailFactory.Production.Api.Api;
using RailFactory.Production.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddProductionHosting();
builder.Services.AddProductionModule();

var app = builder.Build();

app.UseProductionHosting();
app.MapProductionEndpoints();

app.Run();
