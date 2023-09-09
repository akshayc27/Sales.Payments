using Serilog;
using Serilog.Exceptions;
using Serilog.Events;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Formatting.Compact;
using Serilog.Parsing;
using Destructurama;
using System.Reflection;

namespace Sales.Payments
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                throw new InvalidOperationException("Missing required envirement variable ASPNETCORE_ENVIRONMENT.");

            //Here We would need logger to record any startup errors.
            //We will overrite it when we configure final web host.

            Log.Logger = ConfigureSerilog(new LoggerConfiguration(), environment).CreateLogger();

            Log.Information("Logging Configured");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });


        private static LoggerConfiguration ConfigureSerilog(LoggerConfiguration config, string environment)
        {
            //Environment agnostic setup
            config.Enrich.FromLogContext()
                .Enrich.WithExceptionDetails(
                new DestructuringOptionsBuilder().WithDefaultDestructurers()
                .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() })
                );

            return environment switch
            {
                "Development" => ConfigureDevelopmentLogging(config),
                "Staging" => config.WriteTo.Console(formatter: new RenderedCompactJsonFormatter()),
                "Production" => config.WriteTo.Console(formatter: new RenderedCompactJsonFormatter())
                    .Destructure.UsingAttributes(),
                _ => throw new InvalidOperationException($"Cannot configure logging,unexpected enviroment '{environment}'.")

            };
        }

        static LoggerConfiguration ConfigureDevelopmentLogging(LoggerConfiguration loggerConfiguration)
        {
            var expectedDirectory = Assembly.GetExecutingAssembly().GetName().Name;
            var currentDirectory = Directory.GetCurrentDirectory();

            //if (!currentDirectory.EndsWith(expectedDirectory!))
            //{
            //    throw new InvalidOperationException(
            //        $"Expected Current Diretory to be '{expectedDirectory}' but it is '{currentDirectory}'"
            //        );
            //}

            var logDirectory = Path.Combine(currentDirectory, "logs");

            static bool SourceContextIsEfCommand(LogEvent e)
            {
                if (!e.Properties.TryGetValue("SourceContext", out var propertyValue))
                {
                    return false;
                }
                return propertyValue switch
                {
                    ScalarValue { Value: string value } => value.StartsWith(
                        "Microsoft.EntityFrameworkCore.Database.Command"),
                    _ => false
                };
            }

            static bool LogAllExceptEfCommands(LogEvent e) =>
                !SourceContextIsEfCommand(e) || e.Level >= LogEventLevel.Warning;

            return loggerConfiguration
                .WriteTo.Conditional(LogAllExceptEfCommands,
                writeTo => writeTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate:
                    "[{Timestamp : HH:MM:ss} {Level:u3}] {RequestId} {Message:lj}{NewLine}{Exception}"
                )
                )
                .WriteTo.Conditional(LogAllExceptEfCommands,
                writeTo => writeTo.File(
                    Path.Combine(logDirectory,"log.txt"),
                    outputTemplate:
                    "[{Timestamp : HH:MM:ss.fff} | {Level:u3}] | {RequestMethod} {RequestPath} | {RequestId} | {SourceContext} | {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                )
                )
                .WriteTo.Conditional(SourceContextIsEfCommand,
                writeTo => writeTo.File(
                    Path.Combine(logDirectory, "database.txt"),
                    outputTemplate:
                    "[{Timestamp : HH:MM:ss.fff} | {Level :u3}] | {RequestMethod} {RequestPath} | {RequestId} | {SourceContext} | {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                )
                )
                ;
        }
    }
}
