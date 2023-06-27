namespace RssFeeder.Console.FeedBuilders;

internal class RevolverNewsFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly int _articleMaxCount;
    private readonly Serilog.Events.LogEventLevel _logLevel = Serilog.Events.LogEventLevel.Debug;

    public RevolverNewsFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) :
        base(logger, webUtilities, utilities)
    {
        _articleMaxCount = 1000;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
    {
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

        // Stories section
        var container = document.QuerySelector("div.list-articles");
        var nodes = container.QuerySelectorAll("article.item > div.text > h2.title > a");
        if (nodes != null)
        {
            count = 1;
            foreach (var node in nodes.Take(_articleMaxCount))
            {
                var item = CreateNodeLinks(filters, node, "news feed", count++, feedUrl, false);
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
