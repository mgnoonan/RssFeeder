using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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

            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.Register(c => new CosmosDbRepository("rssfeeder", config.endpoint, config.authKey, Log.Logger)).As<IRepository>();
            builder.RegisterType<RssBootstrap>().As<IRssBootstrap>();
            builder.RegisterType<DrudgeReportFeedBuilder>().Named<IRssFeedBuilder>("drudge-report");
            builder.RegisterType<EagleSlantFeedBuilder>().Named<IRssFeedBuilder>("eagle-slant");
            builder.RegisterType<GenericParser>().As<IArticleParser>();
            builder.RegisterType<WebUtils>().As<IWebUtils>();
            builder.RegisterType<Utils>().As<IUtils>();

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

        static void HandleParserError(IEnumerable<CommandLine.Error> errors, string[] args)
        {
            // Return an error code
            Log.Logger.Error("Invalid arguments: '{@args}'", args);
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
                List<Options> optionsList;
                var repository = container.Resolve<IRepository>();
                var bootstrap = container.Resolve<IRssBootstrap>();

                //if (!string.IsNullOrWhiteSpace(opts.TestDefinition))
                //{
                //    optionsList = new List<Options> { opts };
                //}
                //else if (!string.IsNullOrWhiteSpace(opts.Config))
                //{
                //    // Get the directory of the current executable, all config 
                //    // files should be in this path
                //    string configFile = Path.Combine(Utils.GetAssemblyDirectory(), opts.Config);
                //    Log.Logger.Information("Reading from config file: {configFile}", configFile);

                //    using (StreamReader sr = new StreamReader(configFile))
                //    {
                //        // Read the options in JSON format
                //        string json = sr.ReadToEnd();
                //        Log.Logger.Information("Options: {@options}", json);

                //        // Deserialize into our options class
                //        optionsList = JsonConvert.DeserializeObject<List<Options>>(json);
                //    }
                //}
                //else
                //{
                //    optionsList = repository.GetDocuments<Options>("feeds", q => q.Title.Length > 0);
                //}

                optionsList = repository.GetDocuments<Options>("feeds", q => q.Title.Length > 0);

                foreach (var option in optionsList)
                {
                    // Transform the option to the old style feed
                    var f = new RssFeed
                    {
                        Title = option.Title,
                        Description = option.Description,
                        OutputFile = option.OutputFile,
                        Language = option.Language,
                        Url = option.Url,
                        CustomParser = option.CustomParser,
                        Filters = option.Filters.ToList(),
                        CollectionName = option.CollectionName
                    };

                    using (LogContext.PushProperty("collectionName", f.CollectionName))
                    {
                        bootstrap.Start(container, profiler, f);
                    }
                }

                Log.Logger.Information("Profiler results: {results}", profiler.RenderPlainText());
                Log.Logger.Information("END: Completed successfully");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error during processing");
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
