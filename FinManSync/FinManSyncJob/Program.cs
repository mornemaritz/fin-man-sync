// See https://aka.ms/new-console-template for more information
using FinManSync;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .AddUserSecrets<Program>() // Loads user-secrets for this assembly
    .AddEnvironmentVariables();

var configuration = builder.Build();

var result = await new Operator(configuration).OperateAsync(args);
Console.WriteLine(result);

partial class Program
{
    // This partial class can be used to add additional methods or properties if needed.
    // Currently, it only contains the Main method.
}
