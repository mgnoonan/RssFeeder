using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RssFeeder.Console.TagParsers;
using Serilog;

namespace RssFeeder.Tests.Console;

/// <summary>
/// Verifies that all named ITagParser keyed registrations are fully instantiable from a
/// properly-wired ServiceProvider.  This protects the runtime seam in
/// ArticleParser and ParseCommand where ITagParserResolver.Resolve(key) is called.
///
/// These tests perform an actual resolution (including construction) rather than
/// inspecting registration metadata.  A missing or misconfigured transitive dependency
/// would pass metadata-only checks but fail here.
/// </summary>
public class TagParserResolutionTests
{
    [Theory]
    [InlineData("adaptive-parser",  typeof(AdaptiveTagParser))]
    [InlineData("generic-parser",   typeof(GenericTagParser))]
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

    [Fact]
    public void ArticleParser_HardcodedFallbackKey_ResolvesSuccessfully()
    {
        // ArticleParser.GetRouteMatchedTagParser falls back to "adaptive-parser" in three
        // branches: null definition, missing catch-all route template, and no route templates.
        // This test locks in that key as always-resolvable from a properly-wired provider.
        using var provider = BuildServiceProvider();

        var parser = provider.GetRequiredKeyedService<ITagParser>("adaptive-parser");

        Assert.IsAssignableFrom<ITagParser>(parser);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

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
