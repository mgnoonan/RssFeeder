using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace RssFeeder.Mvc.Services;

public class FileFeedDefinitionProvider : IFeedDefinitionProvider
{
    private readonly IReadOnlyList<FeedModel> _feeds;

    public FileFeedDefinitionProvider(IOptions<FeedDefinitionOptions> options, IWebHostEnvironment environment)
    {
        var sourceFile = options.Value.SourceFile;
        var resolvedPath = Path.IsPathRooted(sourceFile)
            ? sourceFile
            : Path.Combine(environment.ContentRootPath, sourceFile);

        _feeds = System.Text.Json.JsonSerializer.Deserialize<List<FeedModel>>(
            File.ReadAllText(resolvedPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FeedModel>();
    }

    public IReadOnlyList<FeedModel> GetFeeds()
    {
        return _feeds;
    }

    public FeedModel GetFeed(string collectionName)
    {
        return _feeds.FirstOrDefault(feed => string.Equals(feed.collectionname, collectionName, StringComparison.OrdinalIgnoreCase));
    }

    public bool FeedExists(string collectionName)
    {
        return GetFeed(collectionName) is not null;
    }
}