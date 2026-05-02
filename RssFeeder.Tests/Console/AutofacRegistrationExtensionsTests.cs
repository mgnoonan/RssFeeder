using Autofac;
using Autofac.Core;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.TagParsers;

namespace RssFeeder.Tests.Console;

public class AutofacRegistrationExtensionsTests
{
    [Fact]
    public void RegisterRssFeederConsoleNamedServices_RegistersExpectedFeedBuilders()
    {
        var container = BuildContainerWithNamedServices();

        var expectedBuilders = new Dictionary<string, string>
        {
            ["drudge-report"] = "DrudgeReportFeedBuilder",
            ["liberty-daily"] = "LibertyDailyFeedBuilder",
            ["bongino-report"] = "BonginoReportFeedBuilder",
            ["citizen-freepress"] = "CitizenFreePressFeedBuilder",
            ["rantingly"] = "RantinglyFeedBuilder",
            ["gutsmack"] = "GutSmackFeedBuilder",
            ["populist-press"] = "PopulistPressFeedBuilder",
            ["bad-blue"] = "BadBlueFeedBuilder",
            ["revolver-news"] = "RevolverNewsFeedBuilder",
            ["freedom-press"] = "FreedomPressFeedBuilder",
            ["conservagator"] = "ConservagatorFeedBuilder",
            ["noah-report"] = "NoahReportFeedBuilder",
            ["protrump-news"] = "ProTrumpNewsFeedBuilder",
            ["off-the-press"] = "OffThePressFeedBuilder",
            ["rubin-report"] = "RubinReportFeedBuilder",
            ["whatfinger"] = "WhatFingerFeedBuilder",
            ["political-signal"] = "PoliticalSignalFeedBuilder",
            ["twitchy"] = "TwitchyFeedBuilder",
            ["parkinsons-news-today"] = "ParkinsonsNewsTodayFeedBuilder"
        };

        Assert.All(
            expectedBuilders,
            builder => Assert.Equal(builder.Value, GetRegisteredLimitTypeName(container, builder.Key, typeof(IRssFeedBuilder))));
    }

    [Fact]
    public void RegisterRssFeederConsoleNamedServices_RegistersExpectedTagParsers()
    {
        var container = BuildContainerWithNamedServices();

        var expectedParsers = new Dictionary<string, string>
        {
            ["generic-parser"] = "GenericTagParser",
            ["adaptive-parser"] = "AdaptiveTagParser",
            ["alltags-parser"] = "AllTagsParser",
            ["script-parser"] = "ScriptTagParser",
            ["htmltag-parser"] = "HtmlTagParser",
            ["jsonldtag-parser"] = "JsonLdTagParser"
        };

        Assert.All(
            expectedParsers,
            parser => Assert.Equal(parser.Value, GetRegisteredLimitTypeName(container, parser.Key, typeof(ITagParser))));
    }

    private static IContainer BuildContainerWithNamedServices()
    {
        var builder = new ContainerBuilder();
        InvokeRegisterNamedServices(builder);
        return builder.Build();
    }

    private static string GetRegisteredLimitTypeName(IContainer container, string name, Type serviceType)
    {
        var registration = container.ComponentRegistry.RegistrationsFor(new KeyedService(name, serviceType)).Single();
        return registration.Activator.LimitType.Name;
    }

    private static void InvokeRegisterNamedServices(ContainerBuilder builder)
    {
        var extensionType = typeof(RssFeeder.Console.AutofacCommandCreator).Assembly
            .GetType("RssFeeder.Console.AutofacRegistrationExtensions", throwOnError: true)!;
        var method = extensionType.GetMethod(
            "RegisterRssFeederConsoleNamedServices",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;

        method.Invoke(null, [builder]);
    }
}