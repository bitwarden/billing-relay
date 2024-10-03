using Billing.Options;

var builder = WebApplication.CreateBuilder(args);

builder.UseBitwardenDefaults();

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.Configure<GlobalSettingsOptions>(builder.Configuration.GetSection("globalSettings"));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
