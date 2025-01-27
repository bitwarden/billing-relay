using Billing.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.Configure<GlobalSettingsOptions>(builder.Configuration.GetSection("globalSettings"));

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false
});

app.MapHealthChecks("/alive", new HealthCheckOptions
{
    AllowCachingResponses = false
});

app.Run();
