namespace RssFeeder.Console.FeedBuilders;

internal class RevolverNewsFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly int _articleMaxCount;
    private readonly IUnlaunchClient _unlaunchClient;

    public RevolverNewsFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) :
        base(logger, webUtilities, utilities)
    {
        _articleMaxCount = 1000;
        _unlaunchClient = unlaunchClient;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to log activity
        string key = "feed-log-level";
        string identity = feed.CollectionName;
        string variation = _unlaunchClient.GetVariation(key, identity);
        _log.Information("Unlaunch {key} returned variation {variation} for identity {identity}", key, variation, identity);

        _logLevel = variation switch
        {
            "debug" => Serilog.Events.LogEventLevel.Debug,
            "information" => Serilog.Events.LogEventLevel.Information,
            _ => throw new ArgumentException("Unexpected variation")
        };

        return GenerateRssFeedItemList(feed.CollectionName, feed.Url, feed.Filters, html);
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
    {
        _feedFilters = feedFilters ?? new List<string>();
        _feedUrl = feedUrl ?? string.Empty;

        var items = GenerateRssFeedItemList(html);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html)
    {
        var list = new List<RssFeedItem>();
        int count;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Stories section
        var container = document.QuerySelector("div.list-articles");
        var nodes = container.QuerySelectorAll("article.item > div.text > h2.title > a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes.Take(_articleMaxCount))
            {
                var item = CreateNodeLinks(_feedFilters, node, "news feed", count++, _feedUrl, false);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
