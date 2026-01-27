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

        // Picks section
        // #hero > div > div > div.col-md-4 > div.feature-article > h1 > a
        GetNodeLinks("picks", "div.feature-article > h1.lead-story_title", "a", list, false);
        // #hero > div > div > div.col-md-4 > div.row > div > div > ul > li:nth-child(1) > a
        GetNodeLinks("picks", "#hero > div > div > div.col-md-4 > div.row > div > div", "a", list, false);

        // Top Stories section
        // #home-top-stories > ul > li:nth-child(2) > a
        GetNodeLinks("top stories", "#home-top-stories > ul", "a", list, false);

        // Trending videos
        // #page-wrapper > section.stories-video-redesign.px-4.pt-4 > div > div > div > div:nth-child(1) > a
        GetNodeLinks("trending videos", "#page-wrapper > section.stories-video-redesign > div > div > div", "a", list, false);

        // Capitol Hill stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(1) > ul > li:nth-child(1) > a
        GetNodeLinks("column 1", "#page-wrapper > section.all-stories > div > div > div:nth-child(1)", "a", list, false);

        // Culture War stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(2) > ul:nth-child(2) > li:nth-child(1) > a
        GetNodeLinks("column 2", "#page-wrapper > section.all-stories > div > div > div:nth-child(2)", "a", list, false);

        // Culture War stories
        // body > div > section.all-stories > div > div > div:nth-child(3)
        GetNodeLinks("column 3", "#page-wrapper > section.all-stories > div > div > div:nth-child(3)", "a", list, false);

        // Swamp Watch stories
        // body > div > section.all-stories > div > div > div:nth-child(8)
        GetNodeLinks("column 4", "#page-wrapper > section.all-stories > div > div > div:nth-child(4)", "a", list, false);

        // National Security stories
        // body > div > section.all-stories > div > div > div:nth-child(9)
        GetNodeLinks("column 5", "#page-wrapper > section.all-stories > div > div > div:nth-child(5)", "a", list, false);

        // Opinion stories
        // body > div > section.all-stories > div > div > div:nth-child(10)
        GetNodeLinks("column 6", "#page-wrapper > section.all-stories > div > div > div:nth-child(6)", "a", list, false);

        // Entertainment stories
        // body > div > section.all-stories > div > div > div:nth-child(14)
        GetNodeLinks("column 7", "#page-wrapper > section.all-stories > div > div > div:nth-child(7)", "a", list, false);

        // Sports stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(8) > ul > li:nth-child(1) > a
        GetNodeLinks("column 8", "#page-wrapper > section.all-stories > div > div > div:nth-child(8)", "a", list, false);

        return list;
    }
}
