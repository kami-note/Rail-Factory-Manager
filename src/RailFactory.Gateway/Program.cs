using RailFactory.Gateway.Api;
using RailFactory.Gateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddGatewayHosting();

var app = builder.Build();

app.UseGatewayHosting();
app.MapGatewayEndpoints();

app.Run();
