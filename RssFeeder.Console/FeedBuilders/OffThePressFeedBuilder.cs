namespace RssFeeder.Console.FeedBuilders;

internal class OffThePressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;

    public OffThePressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
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

        var items = GenerateRssFeedItemList(html);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html)
    {
        var list = new List<RssFeedItem>();

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        // #main > div.section.features > div > div > div.col-md-offset-1.col-sm-offset-1.col-xs-offset-0.col-md-10.col-sm-10.col-xs-12.featured-post > a:nth-child(3)
        GetNodeLinks(document, "main headlines", "div.featured-post", "a", list);

        // Posts section
        GetNodeLinks(document, "posts", "#post-list", "a", list);

        return list;
    }
}
