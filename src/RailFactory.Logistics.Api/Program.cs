using RailFactory.Logistics.Api.Api;
using RailFactory.Logistics.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddLogisticsHosting();
builder.Services.AddLogisticsModule(builder.Configuration);

var app = builder.Build();
app.UseLogisticsHosting();
app.MapLogisticsEndpoints();
app.Run();
