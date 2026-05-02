using RssFeeder.Console;
using RssFeeder.Tests.Infrastructure;
using Serilog;

namespace RssFeeder.Tests.Console;

/// <summary>
/// Tests the early-exit paths in ArticleParser.Parse() that return before any
/// _container.ResolveNamed&lt;ITagParser&gt;() call is made.
///
/// By initializing ArticleParser with a null container we prove that these paths
/// never touch the container — any accidental container access would throw a
/// NullReferenceException and the test would fail, surfacing the regression.
/// </summary>
public class ArticleParserContainerSeamTests
{
    private readonly ArticleParser _parser;

    public ArticleParserContainerSeamTests()
    {
        _parser = new ArticleParser();
        // Null resolver is intentional: early-exit paths must not access it.
        _parser.Initialize(
            tagParserResolver: null,
            definitionFactory: null,
            webUtils: null,
            log: new LoggerConfiguration().CreateLogger());
    }

    [Fact]
    public void Parse_WhenFileDoesNotExist_ReturnsWithoutContainerResolution()
    {
        var item = new RssFeeder.Models.RssFeedItem();
        item.FeedAttributes.FileName = Path.Combine(Path.GetTempPath(), "does-not-exist.html");

        // Should not throw despite container being null
        _parser.Parse(item);
    }

    [Theory]
    [InlineData(".json")]
    [InlineData(".txt")]
    public void Parse_WhenTextFile_ReturnsWithoutContainerResolution(string extension)
    {
        using var workspace = new TestWorkspace();
        string path = workspace.CreateFile($"article{extension}", "<html/>");

        var item = new RssFeeder.Models.RssFeedItem();
        item.FeedAttributes.FileName = path;

        _parser.Parse(item);
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".gif")]
    [InlineData(".pdf")]
    [InlineData(".mp3")]
    [InlineData(".webp")]
    public void Parse_WhenBinaryFile_ReturnsWithoutContainerResolution(string extension)
    {
        using var workspace = new TestWorkspace();
        string path = workspace.CreateFile($"article{extension}", "binary-content");

        var item = new RssFeeder.Models.RssFeedItem();
        item.FeedAttributes.FileName = path;

        _parser.Parse(item);
    }
}
