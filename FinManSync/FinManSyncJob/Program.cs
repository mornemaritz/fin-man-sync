// See https://aka.ms/new-console-template for more information
using FinManSync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder = new ConfigurationBuilder()
    .AddUserSecrets<Program>() // Loads user-secrets for this assembly
    .AddEnvironmentVariables();

var configuration = builder.Build();

using var loggerFactory = LoggerFactory.Create(logging =>
{
    // Add/replace providers here to log to other destinations (file, Application Insights, etc.)
    // without changing any code in Operator.
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Operator>();

var result = await new Operator(configuration, logger).OperateAsync(args);
logger.LogInformation("{Result}", result);

partial class Program
{
    // This partial class can be used to add additional methods or properties if needed.
    // Currently, it only contains the Main method.
}
