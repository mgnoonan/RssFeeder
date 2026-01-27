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

        // Headlines section
        // #hero > div > div > div.col-md-4 > div.feature-article > h1 > a
        GetNodeLinks("headlines", "div.feature-article > h1.lead-story_title", "a", list, false);

        // Today's Picks section
        // #hero > div > div > div.col-md-4 > div.row > div > div > ul > li:nth-child(1) > a
        GetNodeLinks("today's picks", "#hero > div > div > div.col-md-4 > div.row > div > div", "a", list, false);

        // Top Stories section
        // #home-top-stories > ul > li:nth-child(2) > a
        GetNodeLinks("top stories", "#home-top-stories > ul", "a", list, false);

        // Trending Videos
        // #page-wrapper > section.stories-video-redesign.px-4.pt-4 > div > div > div > div:nth-child(1) > a
        GetNodeLinks("trending videos", "#page-wrapper > section.stories-video-redesign > div > div > div", "a", list, false);

        // Capitol Hill stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(1) > ul > li:nth-child(1) > a
        GetNodeLinks("capitol hill", "#page-wrapper > section.all-stories > div > div > div:nth-child(1)", "a", list, false);

        // Economy stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(2) > ul:nth-child(2) > li:nth-child(1) > a
        GetNodeLinks("economy", "#page-wrapper > section.all-stories > div > div > div:nth-child(2)", "a", list, false);

        // Swamp Watch stories
        // body > div > section.all-stories > div > div > div:nth-child(3)
        GetNodeLinks("swamp watch", "#page-wrapper > section.all-stories > div > div > div:nth-child(3)", "a", list, false);

        // NatSec stories
        // body > div > section.all-stories > div > div > div:nth-child(8)
        GetNodeLinks("natsec & global affairs", "#page-wrapper > section.all-stories > div > div > div:nth-child(4)", "a", list, false);

        // Sports stories
        // body > div > section.all-stories > div > div > div:nth-child(9)
        GetNodeLinks("sports & entertainment", "#page-wrapper > section.all-stories > div > div > div:nth-child(5)", "a", list, false);

        // Science stories
        // body > div > section.all-stories > div > div > div:nth-child(10)
        GetNodeLinks("science & tech", "#page-wrapper > section.all-stories > div > div > div:nth-child(6)", "a", list, false);

        // Health stories
        // body > div > section.all-stories > div > div > div:nth-child(14)
        GetNodeLinks("health & fitness", "#page-wrapper > section.all-stories > div > div > div:nth-child(7)", "a", list, false);

        // Opinion stories
        // #page-wrapper > section.all-stories.mt-4 > div > div > div:nth-child(8) > ul > li:nth-child(1) > a
        GetNodeLinks("opinion", "#page-wrapper > section.all-stories > div > div > div:nth-child(8)", "a", list, false);

        return list;
    }
}
