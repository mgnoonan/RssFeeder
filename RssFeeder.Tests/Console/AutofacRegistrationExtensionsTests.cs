using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.TagParsers;
using Serilog;

namespace RssFeeder.Tests.Console;

public class KeyedServiceRegistrationTests
{
    [Theory]
    [InlineData("drudge-report",         "DrudgeReportFeedBuilder")]
    [InlineData("liberty-daily",         "LibertyDailyFeedBuilder")]
    [InlineData("bongino-report",        "BonginoReportFeedBuilder")]
    [InlineData("citizen-freepress",     "CitizenFreePressFeedBuilder")]
    [InlineData("rantingly",             "RantinglyFeedBuilder")]
    [InlineData("gutsmack",              "GutSmackFeedBuilder")]
    [InlineData("populist-press",        "PopulistPressFeedBuilder")]
    [InlineData("bad-blue",              "BadBlueFeedBuilder")]
    [InlineData("revolver-news",         "RevolverNewsFeedBuilder")]
    [InlineData("freedom-press",         "FreedomPressFeedBuilder")]
    [InlineData("conservagator",         "ConservagatorFeedBuilder")]
    [InlineData("noah-report",           "NoahReportFeedBuilder")]
    [InlineData("protrump-news",         "ProTrumpNewsFeedBuilder")]
    [InlineData("off-the-press",         "OffThePressFeedBuilder")]
    [InlineData("rubin-report",          "RubinReportFeedBuilder")]
    [InlineData("whatfinger",            "WhatFingerFeedBuilder")]
    [InlineData("political-signal",      "PoliticalSignalFeedBuilder")]
    [InlineData("twitchy",               "TwitchyFeedBuilder")]
    [InlineData("parkinsons-news-today", "ParkinsonsNewsTodayFeedBuilder")]
    public void ResolveKeyed_FeedBuilder_ReturnsExpectedConcreteType(string key, string expectedTypeName)
    {
        using var provider = BuildServiceProvider();

        var feedBuilder = provider.GetRequiredKeyedService<IRssFeedBuilder>(key);

        Assert.Equal(expectedTypeName, feedBuilder.GetType().Name);
    }

    [Theory]
    [InlineData("generic-parser",   typeof(GenericTagParser))]
    [InlineData("adaptive-parser",  typeof(AdaptiveTagParser))]
    [InlineData("alltags-parser",   typeof(AllTagsParser))]
    [InlineData("script-parser",    typeof(ScriptTagParser))]
    [InlineData("htmltag-parser",   typeof(HtmlTagParser))]
    [InlineData("jsonldtag-parser", typeof(JsonLdTagParser))]
    public void ResolveKeyed_TagParser_ReturnsExpectedConcreteType(string key, Type expectedType)
    {
        using var provider = BuildServiceProvider();

        var parser = provider.GetRequiredKeyedService<ITagParser>(key);

        Assert.IsType(expectedType, parser);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        ILogger logger = new LoggerConfiguration().CreateLogger();

        InvokeAddRssFeederConsoleServices(services, logger);

        return services.BuildServiceProvider();
    }

    private static void InvokeAddRssFeederConsoleServices(IServiceCollection services, ILogger logger)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CrawlerConfig:Source"] = "File"
            })
            .Build();

        var extensionType = typeof(RssFeeder.Console.Database.CrawlerConfigProvider).Assembly
            .GetType("RssFeeder.Console.ServiceRegistrationExtensions", throwOnError: true)!;
        var method = extensionType.GetMethod(
            "AddRssFeederConsoleServices",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        method.Invoke(null, [services, configuration, logger]);
    }
}