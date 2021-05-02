using System;
using System.IO;
using System.Net;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Oakton;
using Oakton.Help;
using Raven.Client.Documents;
using RssFeeder.Console.ArticleDefinitions;
using RssFeeder.Console.ArticleParsers;
using RssFeeder.Console.Commands;
using RssFeeder.Console.Database;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

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
            builder.RegisterType<GutSmackFeedBuilder>().Named<IRssFeedBuilder>("gutsmack");
            builder.RegisterType<GenericParser>().Named<IArticleParser>("generic-parser");
            builder.RegisterType<AdaptiveParser>().Named<IArticleParser>("adaptive-parser");
            builder.RegisterType<WebUtils>().As<IWebUtils>().SingleInstance();
            builder.RegisterType<Utils>().As<IUtils>().SingleInstance();
            builder.RegisterType<ArticleDefinitionFactory>().As<IArticleDefinitionFactory>().SingleInstance();
            builder.RegisterType<TestCommand>().SingleInstance();
            builder.RegisterType<TestInput>().SingleInstance();
            builder.RegisterType<BuildCommand>().SingleInstance();
            builder.RegisterType<BuildInput>().SingleInstance();
            builder.RegisterType<HelpInput>().SingleInstance();

            var container = builder.Build();
            var serviceProvider = new AutofacServiceProvider(container);

            // set up TLS defaults
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var executor = CommandExecutor.For(_ =>
            {
                // Find and apply all command classes discovered
                // in this assembly
                _.RegisterCommands(typeof(Program).GetTypeInfo().Assembly);
            }, new AutofacCommandCreator(container));
            executor.Execute(args);
        }
    }
}
