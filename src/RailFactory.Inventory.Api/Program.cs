using RailFactory.Inventory.Api.Api;
using RailFactory.Inventory.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddInventoryHosting();
builder.Services.AddInventoryModule(builder.Configuration);

var app = builder.Build();

app.UseInventoryHosting();
app.MapInventoryEndpoints();

app.Run();
