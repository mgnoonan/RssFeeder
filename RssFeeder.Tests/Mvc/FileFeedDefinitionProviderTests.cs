using Microsoft.Extensions.Options;
using RssFeeder.Mvc.Models;
using RssFeeder.Mvc.Services;
using RssFeeder.Tests.Infrastructure;

namespace RssFeeder.Tests.Mvc;

public class FileFeedDefinitionProviderTests
{
    [Fact]
    public void Constructor_LoadsRelativeFileFromContentRoot_AndSupportsCaseInsensitiveLookup()
    {
        using var workspace = new TestWorkspace();
        workspace.CreateFile(
            "feeds.json",
            """
            [
              {
                "id": "drudge",
                "title": "Drudge Report",
                "url": "https://example.com",
                "description": "Example feed",
                "outputfile": "drudge.xml",
                "language": "en-us",
                "customparser": "custom",
                "filters": ["news"],
                "collectionname": "Drudge-Report"
              }
            ]
            """);

        var provider = new FileFeedDefinitionProvider(
            Options.Create(new FeedDefinitionOptions { SourceFile = "feeds.json" }),
            new TestWebHostEnvironment { ContentRootPath = workspace.RootPath });

        var feed = provider.GetFeed("drudge-report");

        Assert.Single(provider.GetFeeds());
        Assert.NotNull(feed);
        Assert.True(provider.FeedExists("DRUDGE-REPORT"));
        Assert.Equal("Drudge Report", feed!.title);
    }

    [Fact]
    public void Constructor_LoadsAbsolutePathWhenProvided()
    {
        using var workspace = new TestWorkspace();
        var absolutePath = workspace.CreateFile(
            "nested/feeds.json",
            """
            [
              {
                "id": "daily",
                "title": "Daily Feed",
                "url": "https://daily.example",
                "description": "Daily feed",
                "outputfile": "daily.xml",
                "language": "en-us",
                "customparser": "custom",
                "filters": [],
                "collectionname": "daily"
              }
            ]
            """);

        var provider = new FileFeedDefinitionProvider(
            Options.Create(new FeedDefinitionOptions { SourceFile = absolutePath }),
            new TestWebHostEnvironment { ContentRootPath = Path.Combine(workspace.RootPath, "unused") });

        var feed = provider.GetFeed("daily");

        Assert.NotNull(feed);
        Assert.Equal("https://daily.example", feed!.url);
    }
}