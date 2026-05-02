using Autofac;
using RssFeeder.Console.HttpClients;
using RssFeeder.Console.TagParsers;
using RssFeeder.Console.Utility;
using Serilog;

namespace RssFeeder.Tests.Console;

/// <summary>
/// Verifies that all named ITagParser registrations are fully instantiable from a
/// properly-wired Autofac container.  This protects the runtime seam in
/// ArticleParser.Parse() and ParseCommand.Execute() where
/// _container.ResolveNamed&lt;ITagParser&gt;(key) is called.
///
/// These tests differ from AutofacRegistrationExtensionsTests in that they perform
/// an actual Autofac resolution (including construction) rather than inspecting
/// registration metadata.  A missing or misconfigured transitive dependency would
/// pass the metadata tests but fail here.
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
    public void ResolveNamed_TagParser_ReturnsExpectedConcreteType(string key, Type expectedType)
    {
        var container = BuildFullContainer();

        var parser = container.ResolveNamed<ITagParser>(key);

        Assert.IsType(expectedType, parser);
    }

    [Fact]
    public void ArticleParser_HardcodedFallbackKey_ResolvesSuccessfully()
    {
        // ArticleParser.GetRouteMatchedTagParser falls back to "adaptive-parser" in three
        // branches: null definition, missing catch-all route template, and no route templates.
        // This test locks in that key as always-resolvable from a properly-wired container.
        var container = BuildFullContainer();

        var parser = container.ResolveNamed<ITagParser>("adaptive-parser");

        Assert.IsAssignableFrom<ITagParser>(parser);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static IContainer BuildFullContainer()
    {
        var builder = new ContainerBuilder();

        // Tag parsers all require ILogger and IWebUtils.
        // IWebUtils (WebUtils) additionally requires IHttpClient.
        ILogger logger = new LoggerConfiguration().CreateLogger();
        builder.RegisterInstance(logger).As<ILogger>();
        builder.RegisterType<RestSharpHttpClient>().As<IHttpClient>();
        builder.RegisterType<WebUtils>().As<IWebUtils>();

        InvokeRegisterNamedServices(builder);
        return builder.Build();
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
