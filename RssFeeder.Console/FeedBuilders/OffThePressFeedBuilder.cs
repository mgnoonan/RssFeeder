namespace RssFeeder.Console.FeedBuilders;

internal class OffThePressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public OffThePressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
        int count;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        // #content > div > div.elementor.elementor-24 > div > div > section.elementor-section.elementor-top-section.elementor-element.elementor-element-5a300b6.elementor-section-stretched.elementor-section-boxed.elementor-section-height-default.elementor-section-height-default > div > div > div > div > div > div.elementor-element.elementor-element-18c69d5.elementor-grid-1.elementor-posts--thumbnail-none.elementor-posts--align-center.elementor-grid-tablet-1.elementor-grid-mobile-1.elementor-widget.elementor-widget-posts > div > div > article > div > h3 > a
        //var container = document.QuerySelector("div.page-content > div.elementor > div.elementor-inner");
        //if (container != null)
        //{
        //    var nodes = container.QuerySelectorAll("article > div > h3 > a");
        //    count = 1;
        //    foreach (var node in nodes)
        //    {
        //        var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, false);
        //        if (item != null)
        //        {
        //            log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
        //            list.Add(item);
        //        }
        //    }
        //}

        // Column 1
        var container = document.QuerySelector("#post-list");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "column 1", count++, feedUrl, false);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}
