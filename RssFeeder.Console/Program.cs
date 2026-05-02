using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

var ravenDbOptions = configuration.GetSection(RavenDbOptions.SectionName).Get<RavenDbOptions>() ?? new RavenDbOptions();
var crawlerConfigOptions = configuration.GetSection(CrawlerConfigOptions.SectionName).Get<CrawlerConfigOptions>() ?? new CrawlerConfigOptions();

if (!crawlerConfigOptions.Source.HasValue)
{
#if DEBUG
    crawlerConfigOptions.Source = CrawlerConfigSource.File;
#else
    crawlerConfigOptions.Source = CrawlerConfigSource.RavenDb;
#endif
}

crawlerConfigOptions.FilePath ??= "crawlerconfig.json";

// Init Serilog
// docker run --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
var log = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
Log.Logger = log;

var services = new ServiceCollection();
services.AddRssFeederConsoleServices(configuration, Log.Logger);

// Setup dependency injection
var builder = new ContainerBuilder();
builder.Populate(services);

// Register resolver instances using the populated container (which implements IServiceProvider)
builder.Register(c => new KeyedServiceTagParserResolver(c.Resolve<IServiceProvider>()))
    .As<ITagParserResolver>()
    .SingleInstance();

builder.Register(c => new KeyedServiceFeedBuilderResolver(c.Resolve<IServiceProvider>()))
    .As<IFeedBuilderResolver>()
    .SingleInstance();

var container = builder.Build();

var executor = CommandExecutor.For(_ =>
{
    // Find and apply all command classes discovered
    // in this assembly
    _.RegisterCommands(typeof(Program).GetTypeInfo().Assembly);
}, new AutofacCommandCreator(container));
executor.Execute(args);

Log.CloseAndFlush();
