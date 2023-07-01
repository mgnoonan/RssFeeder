namespace RssFeeder.Console.FeedBuilders;

class TwitchyFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;

    public TwitchyFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(logger, webUtilities, utilities)
    {
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

        var items = GenerateRssFeedItemList(html, _feedFilters, _feedUrl);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html, List<string> filters, string feedUrl)
    {
        var list = new List<RssFeedItem>();

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        GetNodeLinks(document, "main headlines", "section.pt-2", "div.wp-card-huge__title > a", list);

        // Above the fold section
        //var container = document.QuerySelectorAll("section.container-xl")?.FirstOrDefault();
        GetNodeLinks(document, "above the fold", "body>main>section:nth-child(2)", "div.wp-card__title > a", list);

        // Posts section
        GetNodeLinks(document, "posts", "#post-list", "div.wp-card__title > a", list);

        return list;
    }
}
