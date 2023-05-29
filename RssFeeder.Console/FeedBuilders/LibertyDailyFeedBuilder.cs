namespace RssFeeder.Console.FeedBuilders;

class LibertyDailyFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public LibertyDailyFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
    { }

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
        var doc = new HtmlDocument();
        doc.Load(new StringReader(html));

        // Above the fold headline(s)
        // //div[@class=\"drudgery-top-links\"]/div/a
        var nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-top-links\"]/div/a");
        int count;
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "above the fold", count++, feedUrl, true);
                if (item != null)
                {
                    _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Featured headline(s)
        // //div[@class=\"drudgery-featured\"]/div/a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-featured\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, true);
                if (item != null)
                {
                    _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 1 Articles
        // //div[@class=\"drudgery-column-1\"]/div[@class=\"drudgery-articles\"]/div/a
        // #main > div.drudgery-column-1 > div:nth-child(2) > div:nth-child(1) > a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-1\"]/div[@class=\"drudgery-articles\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "left column", count++, feedUrl, false);
                if (item != null)
                {
                    _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 2 Articles
        // //div[@class=\"drudgery-column-2\"]/div[@class=\"drudgery-articles\"]/div/a
        // #main > div.drudgery-column-2 > div:nth-child(2) > div:nth-child(1) > a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-2\"]/div[@class=\"drudgery-articles\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "middle column", count++, feedUrl, false);
                if (item != null)
                {
                    _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 3 Articles
        // //div[@class=\"drudgery-column-3\"]/div[@class=\"drudgery-articles\"]/div/a
        // #main > div.drudgery-column-3 > div:nth-child(2) > div:nth-child(1) > a
        nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-3\"]/div[@class=\"drudgery-articles\"]/div/a");
        if (nodes != null)
        {
            count = 1;
            foreach (HtmlNode node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "right column", count++, feedUrl, false);
                if (item != null)
                {
                    _log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
