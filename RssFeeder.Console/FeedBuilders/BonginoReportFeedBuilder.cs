namespace RssFeeder.Console.FeedBuilders;

class BonginoReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public BonginoReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities, unlaunchClient)
    {
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
        // Find out which feature flag variation we are using to log activity
        _logLevel = Serilog.Events.LogEventLevel.Debug;

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

        // Top Stories section
        // #hero > div > div > div.col-md-8 > div:nth-child(2) > div:nth-child(1) > div > a
        GetNodeLinks("headlines", "div.feature-article", "h1 > a", list, false);

        // Top Stories section
        // #hero > div > div > div.col-md-8 > div:nth-child(2) > div:nth-child(2) > div
        GetNodeLinks("top stories", "#hero > div > div > div.col-md-8 > div:nth-child(2) > div:nth-child(2) > div.all-stories", "li > a", list, false);

        // Trending videos
        // #hero > div > div > div.col-md-4 > div.all-stories
        GetNodeLinks("trending videos", "#hero > div > div > div.col-md-4 > div.all-stories", "div.col > a", list, false);

        // Capitol Hill stories
        // body > div > section.all-stories > div > div > div:nth-child(1)
        GetNodeLinks("column 1", "body > div > section.all-stories > div > div > div:nth-child(1)", "li > a", list, false);

        // Culture War stories
        // body > div > section.all-stories > div > div > div:nth-child(2)
        GetNodeLinks("column 2", "body > div > section.all-stories > div > div > div:nth-child(2)", "li > a", list, false);

        // Culture War stories
        // body > div > section.all-stories > div > div > div:nth-child(3)
        GetNodeLinks("column 3", "body > div > section.all-stories > div > div > div:nth-child(3)", "li > a", list, false);

        // Swamp Watch stories
        // body > div > section.all-stories > div > div > div:nth-child(8)
        GetNodeLinks("column 4", "body > div > section.all-stories > div > div > div:nth-child(8)", "li > a", list, false);

        // National Security stories
        // body > div > section.all-stories > div > div > div:nth-child(9)
        GetNodeLinks("column 5", "body > div > section.all-stories > div > div > div:nth-child(9)", "li > a", list, false);

        // Opinion stories
        // body > div > section.all-stories > div > div > div:nth-child(10)
        GetNodeLinks("column 6", "body > div > section.all-stories > div > div > div:nth-child(10)", "li > a", list, false);

        // Entertainment stories
        // body > div > section.all-stories > div > div > div:nth-child(14)
        GetNodeLinks("column 7", "body > div > section.all-stories > div > div > div:nth-child(14)", "li > a", list, false);

        // Sports stories
        // body > div > section.all-stories > div > div > div:nth-child(15)
        GetNodeLinks("column 8", "body > div > section.all-stories > div > div > div:nth-child(15)", "li > a", list, false);

        // Health stories
        // body > div > section.all-stories > div > div > div:nth-child(16)
        GetNodeLinks("column 9", "body > div > section.all-stories > div > div > div:nth-child(16)", "li > a", list, false);

        return list;
    }
}
