using CustomerJob;
using CustomerJob.Consumers;
using CustomerJob.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;

Log.Logger = new LoggerConfiguration()
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

try
{
    Log.Information("Starting up ...");

    var builder = Host.CreateApplicationBuilder(args);

    // Load configuration from Secrets or AppSettings

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddUserSecrets<Program>(optional: true);

    #region Settings Validation ServiceBus/Database

    var serviceBusSettings = builder.Configuration.GetSection("AzureServiceBus").Get<ServiceBusSettings>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // Validate ServiceBus settings
    if (string.IsNullOrWhiteSpace(serviceBusSettings?.ConnectionString) || string.IsNullOrWhiteSpace(serviceBusSettings?.QueueName))
    {
        throw new InvalidOperationException("Invalid ServiceBus configuration. Please check your appsettings.json/secrets.");
    }

    // Check if the connection string is not empty
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("DB Connection string is missing or empty. Please check your appsettings.json/secrets.");
    }

    #endregion Settings Validation ServiceBus/Database  

    builder.Services.Configure<ServiceBusSettings>(builder.Configuration.GetSection("AzureServiceBus"));

    // Clear default logging and use Serilog
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();

    builder.Services.AddHostedService<Job>();

    builder.Services.AddSingleton<CustomerConsumer>();
    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
