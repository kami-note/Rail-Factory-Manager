using RailFactory.Frontend.Api;
using RailFactory.Frontend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddFrontendHosting();

var app = builder.Build();
app.UseFrontendHosting();
var staticUi = app.UseFrontendStaticUi();
app.MapFrontendEndpoints(staticUi);

app.Run();

public partial class Program;
