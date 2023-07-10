namespace RssFeeder.Console.FeedBuilders;

class BadBlueFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public BadBlueFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
    {
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to crawl articles
        string key = "article-count-limit";
        string identity = feed.CollectionName;
        string variation = _unlaunchClient.GetVariation(key, identity, new List<UnlaunchAttribute>
        {
            UnlaunchAttribute.NewBoolean("weekend", DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
        });
        _log.Information("Unlaunch {key} returned variation {variation} for identity {identity}", key, variation, identity);

        _articleMaxCount = variation switch
        {
            "high" => 30,
            "medium" => 25,
            "low" => 20,
            "unlimited" => 1000,
            _ => throw new ArgumentException("Unexpected variation")
        };
        _log.Information("Processing a maximum of {articleMaxCount} articles", _articleMaxCount);

        // Find out which feature flag variation we are using to log activity
        key = "feed-log-level";
        identity = feed.CollectionName;
        variation = _unlaunchClient.GetVariation(key, identity);
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
        Initialize(feedUrl, feedFilters, html);
        var items = GenerateRssFeedItemList();
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList()
    {
        var list = new List<RssFeedItem>();

        // Main headlines section
        // body > div.headln1 > div > span.tih1 > a
        GetNodeLinks("headlines", "div.headln1", "span.tih1 > a", list, false);

        // Stories section
        // body > div.grid > div:nth-child(1) > p:nth-child(1) > a
        GetNodeLinks("stories", "div.grid > div.headlines", "p > a", list, false);

        return list;
    }
}
