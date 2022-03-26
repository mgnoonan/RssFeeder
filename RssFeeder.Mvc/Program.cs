using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace RssFeeder.Mvc
{
    public class Program
    {
        public static int Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .ConfigureLogging((hostingContext, logging) => logging.ClearProviders())
                        .UseSerilog((hostingContext, loggerConfiguration) =>
                        {
                            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                            telemetryConfiguration.ConnectionString = hostingContext.Configuration["ApplicationInsights:ConnectionString"];

                            loggerConfiguration
                                .ReadFrom.Configuration(hostingContext.Configuration)
#if DEBUG
                                .MinimumLevel.Debug()
                                .WriteTo.Console()
                                .WriteTo.Debug()
                                .WriteTo.Seq("http://localhost:5341")       // docker run --rm -it -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
#endif
                                .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces);
                        })
                    .UseStartup<Startup>();
                });
    }
}
