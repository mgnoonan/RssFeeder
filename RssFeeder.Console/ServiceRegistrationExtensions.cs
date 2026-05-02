using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oakton.Help;
using RssFeeder.Console.Commands;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.HttpClients;
using RssFeeder.Console.TagParsers;

namespace RssFeeder.Console;

internal static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddRssFeederConsoleServices(this IServiceCollection services, IConfigurationRoot configuration, ILogger logger)
    {
        var ravenDbOptions = configuration.GetSection(RavenDbOptions.SectionName).Get<RavenDbOptions>() ?? new RavenDbOptions();
        var crawlerConfigOptions = configuration.GetSection(CrawlerConfigOptions.SectionName).Get<CrawlerConfigOptions>() ?? new CrawlerConfigOptions();
        var cosmosDbOptions = BindCosmosDbOptions(configuration);

        if (!crawlerConfigOptions.Source.HasValue)
        {
#if DEBUG
            crawlerConfigOptions.Source = CrawlerConfigSource.File;
#else
            crawlerConfigOptions.Source = CrawlerConfigSource.RavenDb;
#endif
        }

        crawlerConfigOptions.FilePath ??= "crawlerconfig.json";

        services.AddSingleton(logger);
        services.AddSingleton(configuration);
        services.AddSingleton(ravenDbOptions);
        services.AddSingleton(crawlerConfigOptions);
        services.AddSingleton(cosmosDbOptions);
        services.AddSingleton<IDocumentStore>(_ => new DocumentStore
        {
            Urls = ravenDbOptions.Urls
        }.Initialize());
        services.AddSingleton<ICrawlerConfigProvider, CrawlerConfigProvider>();
        services.AddSingleton<RavenDbRepository>();
        services.AddSingleton<IRepository>(serviceProvider => serviceProvider.GetRequiredService<RavenDbRepository>());
#if DEBUG
        services.AddSingleton<IExportRepository>(serviceProvider => serviceProvider.GetRequiredService<RavenDbRepository>());
#else
        services.AddSingleton<CosmosDbRepository>();
        services.AddSingleton<IExportRepository>(serviceProvider => serviceProvider.GetRequiredService<CosmosDbRepository>());
#endif
        services.AddSingleton<IArticleExporter, ArticleExporter>();
        services.AddSingleton<IArticleParser, ArticleParser>();
        services.AddSingleton<IWebCrawler, WebCrawler>();
        services.AddSingleton<IHttpClient, RestSharpHttpClient>();
        services.AddSingleton<IWebUtils, WebUtils>();
        services.AddSingleton<IUtils, Utils>();
        services.AddSingleton<IArticleDefinitionFactory, ArticleDefinitionFactory>();
        services.AddSingleton<TestCommand>();
        services.AddSingleton<TestInput>();
        services.AddSingleton<BuildCommand>();
        services.AddSingleton<BuildInput>();
        services.AddSingleton<ParseCommand>();
        services.AddSingleton<ParseInput>();
        services.AddSingleton<DownloadCommand>();
        services.AddSingleton<DownloadInput>();
        services.AddSingleton<CheckRulesCommand>();
        services.AddSingleton<CheckRulesInput>();
        services.AddSingleton<AuditCommand>();
        services.AddSingleton<AuditInput>();
        services.AddSingleton<HelpInput>();

        // Register named feed builders as keyed services
        services.AddKeyedSingleton<IRssFeedBuilder>("drudge-report", (sp, key) => new DrudgeReportFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("liberty-daily", (sp, key) => new LibertyDailyFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("bongino-report", (sp, key) => new BonginoReportFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("citizen-freepress", (sp, key) => new CitizenFreePressFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("rantingly", (sp, key) => new RantinglyFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("gutsmack", (sp, key) => new GutSmackFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("populist-press", (sp, key) => new PopulistPressFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("bad-blue", (sp, key) => new BadBlueFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("revolver-news", (sp, key) => new RevolverNewsFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("freedom-press", (sp, key) => new FreedomPressFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("conservagator", (sp, key) => new ConservagatorFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("noah-report", (sp, key) => new NoahReportFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("protrump-news", (sp, key) => new ProTrumpNewsFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("off-the-press", (sp, key) => new OffThePressFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("rubin-report", (sp, key) => new RubinReportFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("whatfinger", (sp, key) => new WhatFingerFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("political-signal", (sp, key) => new PoliticalSignalFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("twitchy", (sp, key) => new TwitchyFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));
        services.AddKeyedSingleton<IRssFeedBuilder>("parkinsons-news-today", (sp, key) => new ParkinsonsNewsTodayFeedBuilder(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>(), sp.GetRequiredService<IUtils>()));

        // Register named tag parsers as keyed services
        services.AddKeyedSingleton<ITagParser>("generic-parser", (sp, key) => new GenericTagParser(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>()));
        services.AddKeyedSingleton<ITagParser>("adaptive-parser", (sp, key) => new AdaptiveTagParser(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>()));
        services.AddKeyedSingleton<ITagParser>("alltags-parser", (sp, key) => new AllTagsParser(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>()));
        services.AddKeyedSingleton<ITagParser>("script-parser", (sp, key) => new ScriptTagParser(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>()));
        services.AddKeyedSingleton<ITagParser>("htmltag-parser", (sp, key) => new HtmlTagParser(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>()));
        services.AddKeyedSingleton<ITagParser>("jsonldtag-parser", (sp, key) => new JsonLdTagParser(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<IWebUtils>()));

        return services;
    }

    private static CosmosDbOptions BindCosmosDbOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(CosmosDbOptions.SectionName).Get<CosmosDbOptions>() ?? new CosmosDbOptions();
        var configuredDatabaseName = configuration[$"{CosmosDbOptions.SectionName}:DatabaseName"];
        var legacyDatabaseName = configuration["database_id"];

        if (string.IsNullOrWhiteSpace(options.Account) || string.IsNullOrWhiteSpace(options.Key))
        {
            var legacyOptions = configuration.GetSection("CosmosDB").Get<CosmosDbConfig>();
            if (legacyOptions is not null)
            {
                options.Account ??= legacyOptions.endpoint;
                options.Key ??= legacyOptions.authKey;
            }
        }

        if (string.IsNullOrWhiteSpace(configuredDatabaseName) && !string.IsNullOrWhiteSpace(legacyDatabaseName))
        {
            options.DatabaseName = legacyDatabaseName;
        }

        return options;
    }
}