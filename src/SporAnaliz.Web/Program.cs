using SporAnaliz.Web.Endpoints;
using SporAnaliz.Web.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMatchData(builder.Configuration);
builder.Services.AddSingleton<AnalysisService>();

var app = builder.Build();
app.InitializeMatchData();
app.UseDatabaseErrorHandling();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapStatusEndpoints();
app.MapFixtureEndpoints();
app.MapAnalysisEndpoints();
app.MapTeamEndpoints();
app.MapAdminEndpoints();

app.Run();
