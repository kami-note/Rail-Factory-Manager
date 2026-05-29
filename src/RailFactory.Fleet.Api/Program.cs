using RailFactory.Fleet.Api.Api;
using RailFactory.Fleet.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddFleetHosting();
builder.Services.AddFleetModule(builder.Configuration);

var app = builder.Build();

app.UseFleetHosting();
app.MapFleetEndpoints();

app.Run();
