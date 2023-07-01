namespace RssFeeder.Console.FeedBuilders;

class BadBlueFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;
    private int _articleMaxCount;

    public BadBlueFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
    {
        _unlaunchClient = unlaunchClient;
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

        // Main headlines section
        var container = document.QuerySelector("div.storyh1 > span");
        var nodes = container.QuerySelectorAll("a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(_feedFilters, node, "main headlines", count++, _feedUrl, true);
                if (item != null)
                {
                    _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Stories section
        container = document.QuerySelector("div.grid");
        nodes = container.QuerySelectorAll("div.headlines > p > a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes.Take(_articleMaxCount))
            {
                var item = CreateNodeLinks(_feedFilters, node, "all stories", count++, _feedUrl, false);
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
