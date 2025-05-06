using CustomerJob;
using CustomerJob.Consumers;
using CustomerJob.Data;
using CustomerJob.Models.Settings;
using CustomerJob.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = CreateLogger();

try
{
    Log.Information("Starting up ...");

    var builder = Host.CreateApplicationBuilder(args);
    
    ValidateServiceBusConfiguration(builder);

    builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("AzureServiceBus"));

    var environmentName = builder.Environment.EnvironmentName;

    builder.Services.AddDbContext<AppDbContext>(options => 
        options.UseNpgsql(builder.Configuration.GetConnectionString(environmentName.Equals("Test") ? "TestConnection" : "DefaultConnection")));

    // Clear default logging and use Serilog
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    builder.Services.AddHostedService<Job>();
    builder.Services.AddSingleton<CustomerConsumer>();
    builder.Services.AddScoped<IDataStorageService, DataStorageService>();

    var app = builder.Build();

    ApplyMigration(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}


static Serilog.ILogger CreateLogger()
{
    return new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Logger(lc => lc
            .Filter.ByExcluding(log =>
                log.Properties.ContainsKey("SourceContext") &&
                log.Properties["SourceContext"].ToString().Contains("Microsoft.EntityFrameworkCore.Database.Command"))
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
            )
        )
        .WriteTo.File(
            "Logs/log.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug
        )
        .CreateLogger();
}

static void ValidateServiceBusConfiguration(HostApplicationBuilder builder)
{
    var serviceBusSettings = builder.Configuration.GetSection("AzureServiceBus").Get<ServiceBusSettings>();

    if (string.IsNullOrWhiteSpace(serviceBusSettings?.ConnectionString) || string.IsNullOrWhiteSpace(serviceBusSettings?.QueueName))
    {
        throw new InvalidOperationException("Invalid ServiceBus configuration. Please check your appsettings.json/secrets.");
    }
}

static void ApplyMigration(IHost app)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.GetPendingMigrations().Count() > 0)
        {
            db.Database.Migrate();
        }
    }
}