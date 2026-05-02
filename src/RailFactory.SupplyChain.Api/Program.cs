using RailFactory.SupplyChain.Api.Api;
using RailFactory.SupplyChain.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddSupplyChainHosting();
builder.Services.AddSupplyChainModule();

var app = builder.Build();

app.UseSupplyChainHosting();
app.MapSupplyChainEndpoints();

app.Run();
