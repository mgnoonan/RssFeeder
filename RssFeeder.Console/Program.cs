using System.Reflection;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;
using Oakton.Help;
using RssFeeder.Console;
using RssFeeder.Console.Commands;
using RssFeeder.Console.HttpClients;
using RssFeeder.Models.Serialization;

BsonSerializer.RegisterSerializer<JObject>(new JObjectBsonSerializer());

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

// Setup dependency injection
var builder = new ContainerBuilder();

builder.RegisterInstance(Log.Logger).As<ILogger>();
builder.RegisterInstance(configuration).As<IConfigurationRoot>();

bool isAuditMode = args.Length > 0 && string.Equals(args[0], "audit", StringComparison.OrdinalIgnoreCase);
if (isAuditMode)
{
    builder.RegisterType<NullRepository>().As<IRepository>().SingleInstance();
    builder.RegisterType<NullRepository>().As<IExportRepository>().SingleInstance();
}
else
{
    builder.RegisterType<RavenDbRepository>().SingleInstance();
    builder.RegisterType<CosmosDbRepository>().SingleInstance();
    builder.RegisterType<MongoDbRepository>().SingleInstance();

    var repositoryProvider = configuration["Database:RepositoryProvider"]
        ?? configuration["Database:Provider"]
        ?? "RavenDB";
    var exportRepositoryProvider = configuration["Database:ExportRepositoryProvider"]
#if DEBUG
        ?? "RavenDB";
#else
        ?? "CosmosDB";
#endif

    builder.Register(context => repositoryProvider.ToUpperInvariant() switch
        {
            "RAVENDB" => (IRepository)context.Resolve<RavenDbRepository>(),
            "COSMOSDB" => context.Resolve<CosmosDbRepository>(),
            "MONGODB" => context.Resolve<MongoDbRepository>(),
            _ => throw new InvalidOperationException(
                $"Unsupported Database:RepositoryProvider '{repositoryProvider}'. Supported values are RavenDB, CosmosDB, and MongoDB.")
        })
        .As<IRepository>()
        .SingleInstance();

    builder.Register(context => exportRepositoryProvider.ToUpperInvariant() switch
        {
            "RAVENDB" => (IExportRepository)context.Resolve<RavenDbRepository>(),
            "COSMOSDB" => context.Resolve<CosmosDbRepository>(),
            "MONGODB" => context.Resolve<MongoDbRepository>(),
            _ => throw new InvalidOperationException(
                $"Unsupported Database:ExportRepositoryProvider '{exportRepositoryProvider}'. Supported values are RavenDB, CosmosDB, and MongoDB.")
        })
        .As<IExportRepository>()
        .SingleInstance();

    BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
}

builder.RegisterType<ArticleExporter>().As<IArticleExporter>();
builder.RegisterType<ArticleParser>().As<IArticleParser>();
builder.RegisterType<WebCrawler>().As<IWebCrawler>();
builder.RegisterType<GenericFeedBuilder>().As<IRssFeedBuilder>();
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
builder.RegisterType<TestCommand>().SingleInstance();
builder.RegisterType<TestInput>().SingleInstance();
builder.RegisterType<BuildCommand>().SingleInstance();
builder.RegisterType<BuildInput>().SingleInstance();
builder.RegisterType<ParseCommand>().SingleInstance();
builder.RegisterType<ParseInput>().SingleInstance();
builder.RegisterType<DownloadCommand>().SingleInstance();
builder.RegisterType<DownloadInput>().SingleInstance();
builder.RegisterType<CheckRulesCommand>().SingleInstance();
builder.RegisterType<CheckRulesInput>().SingleInstance();
builder.RegisterType<AuditCommand>().SingleInstance();
builder.RegisterType<AuditInput>().SingleInstance();
builder.RegisterType<HelpInput>().SingleInstance();

var container = builder.Build();

var executor = CommandExecutor.For(_ =>
{
    // Find and apply all command classes discovered
    // in this assembly
    _.RegisterCommands(typeof(Program).GetTypeInfo().Assembly);
}, new AutofacCommandCreator(container));
executor.Execute(args);

Log.CloseAndFlush();
