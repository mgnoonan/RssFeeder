namespace RssFeeder.Console.FeedBuilders;

class BonginoReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public BonginoReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
        // #home-top-stories > ul > li > a
        GetNodeLinks("top stories", "#home-top-stories > ul", "li > a", list, false);

        // Trending videos
        // #page-wrapper > section.stories-video-redesign.px-4 > div > div > div > div:nth-child(1) > h6 > a
        GetNodeLinks("trending videos", "#page-wrapper > section.stories-video-redesign > div > div > div > div:nth-child(1)", "h6 > a", list, false);

        // Top News Picks
        // #hero > div > div > div.col-md-4 > div.row > div > div > ul > li > a
        GetNodeLinks("top news picks", "#hero > div > div > div.col-md-4 > div.row > div > div > ul", "li > a", list, false);

        // Capitol Hill stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(1) > ul > li > a
        GetNodeLinks("column 1", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(1) > ul", "li > a", list, false);

        // Economy stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(2) > ul > li > a
        GetNodeLinks("column 2", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(2) > ul", "li > a", list, false);

        // Swamp Watch stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(3) > ul > li > a
        GetNodeLinks("column 3", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(3) > ul", "li > a", list, false);

        // National Security stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(4) > ul > li > a
        GetNodeLinks("column 4", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(4) > ul", "li > a", list, false);

        // Sports & Entertainment stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(5) > ul > li > a
        GetNodeLinks("column 5", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(5) > ul", "li > a", list, false);

        // Science & Tech stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(6) > ul > li > a
        GetNodeLinks("column 6", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(6) > ul", "li > a", list, false);

        // Health & Fitness stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(7) > ul > li > a
        GetNodeLinks("column 7", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(7) > ul", "li > a", list, false);

        // Opinion stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(8) > ul > li > a
        GetNodeLinks("column 8", "#page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(8) > ul", "li > a", list, false);

        return list;
    }
}
