using Billing.Options;
using Serilog;

Log.Logger = HostBuilderExtensions.GetBootstrapLogger();

try
{
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
}
finally
{
    Log.CloseAndFlush();
}
