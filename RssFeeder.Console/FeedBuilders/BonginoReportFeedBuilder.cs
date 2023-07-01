namespace RssFeeder.Console.FeedBuilders;

class BonginoReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;

    public BonginoReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
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
        int count;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Top Stories section
        // //section.banner > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        var container = document.QuerySelector("section.banner");
        if (container != null)
        {
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
        }

        // Top Stories section
        // //section.top-stories > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        container = document.QuerySelector("section.top-stories");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(_feedFilters, node, "top stories", count++, _feedUrl, true);
                    if (item != null)
                    {
                        _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }
        }

        // All Stories section
        // //section.all-stories > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        container = document.QuerySelector("section.all-stories");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(_feedFilters, node, "all stories", count++, _feedUrl, false);
                    if (item != null)
                    {
                        _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }
        }

        // Video Stories section
        // //section.stories-video > div > div > div.col-12.col-sm-8 > div > div > div > h5 > a
        container = document.QuerySelector("section.stories-video");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(_feedFilters, node, "video stories", count++, _feedUrl, false);
                    if (item != null)
                    {
                        _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }
        }

        return list;
    }
}
