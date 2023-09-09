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
        public static int Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                throw new InvalidOperationException("Missing required envirement variable ASPNETCORE_ENVIRONMENT.");

            //Here We would need logger to record any startup errors.
            //We will overrite it when we configure final web host.

            Log.Logger = ConfigureSerilog(new LoggerConfiguration(), environment).CreateLogger();

            Log.Information("Logging configured");

            var host = CreateHostBuilder(args).Build(); //.Run();

            try
            {
                Log.Information("Web host Configured");

                using (var scope = host.Services.CreateScope())
                {
                    var provider = scope.ServiceProvider;

                    Log.Information("Validating Application settings");

                    if(environment == Environments.Development)
                    {
                        Log.Information("Applying migrations");
                        //host.MigrateDbContext<PaymentsDbContext>((_, __) => { });
                    }
                }

                Log.Information("Starting Web Host");
                host.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled Exception in application");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                    .UseSerilog(
                        (context, loggerConfiguration) =>
                              ConfigureSerilog(loggerConfiguration,context.HostingEnvironment.EnvironmentName)
                              .ReadFrom.Configuration(context.Configuration)
                        );
                });
        }

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
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {RequestId} {Message:lj}{NewLine}{Exception}"
                )
                )
                .WriteTo.Conditional(LogAllExceptEfCommands,
                writeTo => writeTo.File(
                    Path.Combine(logDirectory,"log.txt"),
                    outputTemplate:
                    "{Timestamp:HH:mm:ss.fff} | {Level:u3} | {RequestMethod} {RequestPath} | {RequestId} | {SourceContext} | {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                )
                )
                .WriteTo.Conditional(SourceContextIsEfCommand,
                writeTo => writeTo.File(
                    Path.Combine(logDirectory, "database.txt"),
                    outputTemplate:
                    "{Timestamp:HH:mm:ss.fff} | {Level :u3} | {RequestMethod} {RequestPath} | {RequestId} | {SourceContext} | {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day
                )
                )
                ;
        }
    }
}
