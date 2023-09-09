using Serilog;
using Serilog.Exceptions;
using Serilog.Events;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Formatting.Compact;
using Serilog.Parsing;

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

            //Log.Logger = ConfigureSerilog();

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
                //"Development" => ConfigureDevelopmentLogging(config),
                "Staging" => config.WriteTo.Console(formatter: new RenderedCompactJsonFormatter()),

            };
        }
    }
}
