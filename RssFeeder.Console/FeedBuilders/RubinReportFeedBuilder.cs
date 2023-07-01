namespace RssFeeder.Console.FeedBuilders;

internal class RubinReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;
    private Serilog.Events.LogEventLevel _logLevel = Serilog.Events.LogEventLevel.Debug;

    public RubinReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
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
        var items = GenerateRssFeedItemList(html, feedFilters ?? new List<string>(), feedUrl);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html, List<string> filters, string feedUrl)
    {
        var list = new List<RssFeedItem>();
        int count;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        var container = document.QuerySelector("div.elementor-widget-container");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("h1.elementor-post__title > a");
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, true);
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
