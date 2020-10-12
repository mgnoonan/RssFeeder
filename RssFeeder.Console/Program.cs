using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Raven.Client.Documents;
using RssFeeder.Console.ArticleDefinitions;
using RssFeeder.Console.ArticleParsers;
using RssFeeder.Console.Database;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Profiling;

namespace RssFeeder.Console
{
    /// <summary>
    /// A utility to retrieve RSS Feeds from remote servers
    /// </summary>
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Grab the current assembly name
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            // Init Serilog
            // docker run --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
            var log = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(new RenderedCompactJsonFormatter(), "RssFeeder.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();
            Log.Logger = log;

            log.Information("START: Machine: {machineName} Assembly: {assembly}", Environment.MachineName, assemblyName.FullName);

            // Load configuration
            var configBuilder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>()
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = configBuilder.Build();
            var config = new CosmosDbConfig();
            configuration.GetSection("CosmosDB").Bind(config);
            log.Information("Loaded CosmosDB from config. Endpoint='{endpointUri}', authKey='{authKeyPartial}*****'", config.endpoint, config.authKey.Substring(0, 5));

            // Setup dependency injection
            var builder = new ContainerBuilder();

            // Setup RavenDb
            // docker run --rm -d -p 8080:8080 -p 38888:38888 ravendb/ravendb:latest
            IDocumentStore store = new DocumentStore
            {
                Urls = new[] { "http://127.0.0.1:8080/" }
                // Default database is not set
            }.Initialize();

            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.RegisterInstance(store).As<IDocumentStore>();
            builder.Register(c => new CosmosDbRepository("rssfeeder", config.endpoint, config.authKey, Log.Logger)).As<IExportRepository>();
            builder.RegisterType<RavenDbRepository>().As<IRepository>();
            builder.RegisterType<RssBootstrap>().As<IRssBootstrap>();
            builder.RegisterType<DrudgeReportFeedBuilder>().Named<IRssFeedBuilder>("drudge-report");
            builder.RegisterType<EagleSlantFeedBuilder>().Named<IRssFeedBuilder>("eagle-slant");
            builder.RegisterType<LibertyDailyFeedBuilder>().Named<IRssFeedBuilder>("liberty-daily");
            builder.RegisterType<BonginoReportFeedBuilder>().Named<IRssFeedBuilder>("bongino-report");
            builder.RegisterType<CitizenFreePressFeedBuilder>().Named<IRssFeedBuilder>("citizen-freepress");
            builder.RegisterType<RantinglyFeedBuilder>().Named<IRssFeedBuilder>("rantingly");
            builder.RegisterType<GenericParser>().Named<IArticleParser>("generic-parser");
            builder.RegisterType<AdaptiveParser>().Named<IArticleParser>("adaptive-parser");
            builder.RegisterType<WebUtils>().As<IWebUtils>().SingleInstance();
            builder.RegisterType<Utils>().As<IUtils>().SingleInstance();
            builder.RegisterType<ArticleDefinitionFactory>().As<IArticleDefinitionFactory>().SingleInstance();

            var container = builder.Build();
            var serviceProvider = new AutofacServiceProvider(container);

            // set up TLS defaults
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Process the command line arguments
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => ProcessWithExitCode(opts, container))
                .WithNotParsed<Options>(errs => HandleParserError(errs, args))
                ;
        }

        static void HandleParserError(IEnumerable<Error> errors, string[] args)
        {
            // Return an error code
            Log.Logger.Error("Invalid arguments: '{@args}'. Errors: {@errors}", args, errors);
            Environment.Exit(255);
        }

        static void ProcessWithExitCode(Options opts, IContainer container)
        {
            // Zero return value means everything processed normally
            int returnCode = 0;

            // Setup mini profiler
            var profiler = MiniProfiler.StartNew("RssFeeder Profile");

            try
            {
                List<RssFeed> feedList;
                var repository = container.Resolve<IRepository>();
                var bootstrap = container.Resolve<IRssBootstrap>();
                var utils = container.Resolve<IUtils>();
                var webUtils = container.Resolve<IWebUtils>();

                if (!string.IsNullOrWhiteSpace(opts.Config))
                {
                    // Get the directory of the current executable, all config 
                    // files should be in this path
                    string configFile = Path.Combine(utils.GetAssemblyDirectory(), opts.Config);
                    Log.Logger.Information("Reading from config file: {configFile}", configFile);

                    // Read the options in JSON format
                    using StreamReader sr = new StreamReader(configFile);
                    string json = sr.ReadToEnd();
                    Log.Logger.Information("Options: {@options}", json);

                    // Deserialize into our options class
                    feedList = JsonConvert.DeserializeObject<List<RssFeed>>(json);
                }
                else
                {
                    // Get the directory of the current executable, all config 
                    // files should be in this path
                    string configFile = Path.Combine(utils.GetAssemblyDirectory(), "feed-drudge.json");
                    Log.Logger.Information("Reading from config file: {configFile}", configFile);

                    // Read the options in JSON format
                    using StreamReader sr = new StreamReader(configFile);
                    string json = sr.ReadToEnd();
                    Log.Logger.Information("Options: {@options}", json);

                    // Deserialize into our options class
                    feedList = JsonConvert.DeserializeObject<List<RssFeed>>(json);
                }

                foreach (var feed in feedList)
                {
                    if (!feed.Enabled)
                        continue;

                    using (LogContext.PushProperty("collectionName", feed.CollectionName))
                    {
                        bootstrap.Start(container, profiler, feed);
                        bootstrap.Export(container, profiler, feed);
                        bootstrap.Purge(container, profiler, feed);
                    }
                }

                Log.Logger.Information("Profiler results: {results}", profiler.RenderPlainText());
                Log.Logger.Information("END: Completed successfully");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error during processing '{message}'", ex.Message);
                returnCode = 250;
            }
            finally
            {
                Log.CloseAndFlush();
            }

            Environment.Exit(returnCode);
        }
    }
}
