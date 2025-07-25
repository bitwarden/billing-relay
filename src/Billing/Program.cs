using Billing;
using Billing.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.UseBitwardenSdk();
builder.Services.AddSingleton<Observability>();

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.Configure<GlobalSettingsOptions>(builder.Configuration.GetSection("globalSettings"));

builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false
});

app.MapHealthChecks("/alive", new HealthCheckOptions
{
    AllowCachingResponses = false
});

app.Run();
