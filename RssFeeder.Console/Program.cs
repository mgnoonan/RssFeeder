using System.Reflection;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Oakton.Help;
using RssFeeder.Console;
using RssFeeder.Console.Commands;
using RssFeeder.Console.HttpClients;


// Load configuration
var configBuilder = new ConfigurationBuilder()
   .SetBasePath(Directory.GetCurrentDirectory())
   .AddJsonFile("appsettings.json", optional: false)
   .AddUserSecrets<Program>()
   .AddEnvironmentVariables();
IConfigurationRoot configuration = configBuilder.Build();

// Init Serilog
// docker run --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
var log = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
Log.Logger = log;

// Grab the current assembly name
var assemblyName = Assembly.GetExecutingAssembly().Location;
log.Information("START: Machine: {machineName} Assembly: {assembly}", Environment.MachineName, assemblyName);

#if !DEBUG
var config = new CosmosDbConfig();
configuration.GetSection("CosmosDB").Bind(config);
log.Information("Loaded CosmosDB from config. Endpoint='{endpointUri}', authKey='{authKeyPartial}*****'", config.endpoint, config.authKey.Substring(0, 5));
#endif

// Setup dependency injection
var builder = new ContainerBuilder();

// Setup RavenDb
// docker run --rm -d -p 8080:8080 -p 38888:38888 ravendb/ravendb:latest
IDocumentStore store = new DocumentStore
{
    Urls = new[] { "http://127.0.0.1:8080/" }
    // Default database is not set
}.Initialize();

var crawlerConfig = new CrawlerConfig();
using (IDocumentSession session = store.OpenSession(database: "site-parsers"))
{
    crawlerConfig = session.Advanced.RawQuery<CrawlerConfig>("from CrawlerConfig").First();
}
Log.Debug("Crawler config: {@config}", crawlerConfig);

builder.RegisterInstance(Log.Logger).As<ILogger>();
builder.RegisterInstance(store).As<IDocumentStore>();
#if DEBUG
        builder.RegisterType<RavenDbRepository>().As<IExportRepository>();
#else
builder.Register(c => new CosmosDbRepository("rssfeeder", config.endpoint, config.authKey, Log.Logger)).As<IExportRepository>();
#endif
builder.RegisterType<RavenDbRepository>().As<IRepository>();
builder.RegisterType<ArticleExporter>().As<IArticleExporter>().WithProperty("Config", crawlerConfig);
builder.RegisterType<ArticleParser>().As<IArticleParser>().WithProperty("Config", crawlerConfig);
builder.RegisterType<WebCrawler>().As<IWebCrawler>().WithProperty("Config", crawlerConfig);
builder.RegisterType<DrudgeReportFeedBuilder>().Named<IRssFeedBuilder>("drudge-report");
builder.RegisterType<LibertyDailyFeedBuilder>().Named<IRssFeedBuilder>("liberty-daily");
builder.RegisterType<BonginoReportFeedBuilder>().Named<IRssFeedBuilder>("bongino-report");
builder.RegisterType<CitizenFreePressFeedBuilder>().Named<IRssFeedBuilder>("citizen-freepress");
builder.RegisterType<RantinglyFeedBuilder>().Named<IRssFeedBuilder>("rantingly");
builder.RegisterType<GutSmackFeedBuilder>().Named<IRssFeedBuilder>("gutsmack");
builder.RegisterType<PopulistPressFeedBuilder>().Named<IRssFeedBuilder>("populist-press");
builder.RegisterType<BadBlueFeedBuilder>().Named<IRssFeedBuilder>("bad-blue");
builder.RegisterType<RevolverNewsFeedBuilder>().Named<IRssFeedBuilder>("revolver-news");
builder.RegisterType<FreedomPressFeedBuilder>().Named<IRssFeedBuilder>("freedom-press");
builder.RegisterType<ConservagatorFeedBuilder>().Named<IRssFeedBuilder>("conservagator");
builder.RegisterType<NoahReportFeedBuilder>().Named<IRssFeedBuilder>("noah-report");
builder.RegisterType<GenericTagParser>().Named<ITagParser>("generic-parser");
builder.RegisterType<AdaptiveTagParser>().Named<ITagParser>("adaptive-parser");
builder.RegisterType<AllTagsParser>().Named<ITagParser>("alltags-parser");
builder.RegisterType<ScriptTagParser>().Named<ITagParser>("script-parser");
builder.RegisterType<HtmlTagParser>().Named<ITagParser>("htmltag-parser");
builder.RegisterType<JsonLdTagParser>().Named<ITagParser>("jsonldtag-parser");
builder.RegisterType<RestSharpHttpClient>().As<IHttpClient>().SingleInstance();
builder.RegisterType<WebUtils>().As<IWebUtils>().SingleInstance();
builder.RegisterType<Utils>().As<IUtils>().SingleInstance();
builder.RegisterType<ArticleDefinitionFactory>().As<IArticleDefinitionFactory>().SingleInstance();
builder.RegisterType<TestCommand>().SingleInstance().WithProperty("Config", crawlerConfig);
builder.RegisterType<TestInput>().SingleInstance();
builder.RegisterType<BuildCommand>().SingleInstance();
builder.RegisterType<BuildInput>().SingleInstance();
builder.RegisterType<ParseCommand>().SingleInstance();
builder.RegisterType<ParseInput>().SingleInstance();
builder.RegisterType<DownloadCommand>().SingleInstance();
builder.RegisterType<DownloadInput>().SingleInstance();
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
