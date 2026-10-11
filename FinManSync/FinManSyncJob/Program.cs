// See https://aka.ms/new-console-template for more information
using FinManSync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>() // Loads user-secrets for this assembly
    .AddEnvironmentVariables();

var configuration = builder.Build();

// Serilog is configured entirely from the "Serilog" section of appsettings.json
// (sinks, levels, enrichers, output template, rolling policy).
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    // Serilog plugs in as a provider behind Microsoft.Extensions.Logging, so Operator
    // continues to depend only on ILogger<T> and needs no changes.
    using var loggerFactory = LoggerFactory.Create(logging =>
    {
        logging.ClearProviders();
        logging.AddSerilog(Log.Logger, dispose: false);
    });

    var logger = loggerFactory.CreateLogger<Operator>();

    var result = await new Operator(configuration, logger).OperateAsync(args);
    // Write to stdout instead of logging so that the output can be captured by the calling process (e.g., a shell script) if needed.
    Console.WriteLine(result);
}
catch (Exception ex)
{
    Log.Fatal(ex, "FinManSyncJob terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

partial class Program
{
    // This partial class can be used to add additional methods or properties if needed.
    // Currently, it only contains the Main method.
}
