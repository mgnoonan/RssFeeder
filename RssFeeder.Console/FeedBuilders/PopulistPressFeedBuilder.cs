namespace RssFeeder.Console.FeedBuilders;

class PopulistPressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IWebUtils _webUtilities;
    private readonly IUtils _utilities;
    private readonly List<string> _selectors;

    public PopulistPressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
    {
        _webUtilities = webUtilities;
        _utilities = utilities;

        _selectors = new List<string>
            {
                "#outbound_links > a",
                "div.entry-content > p > a"
            };
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

        // Above the Fold section
        var container = document.QuerySelector("#home_page_breaking");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "above the fold", count++, feedUrl, true);
                if (item != null)
                {
                    //TryParseEmbeddedUrl(item, _selectors);
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Main Headlines section
        container = document.QuerySelector("#home_page_featured");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl, true);
                    if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                    {
                        //TryParseEmbeddedUrl(item, _selectors);
                        log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }
        }

        // Column 1
        container = document.QuerySelector("#column_1");
        if (container != null)
        {
            var pairedContainers = container.QuerySelectorAll("ul > li > h2");
            count = 1;
            foreach (var pairedContainer in pairedContainers)
            {
                var nodeTitle = pairedContainer.QuerySelector("span.mf-headline > a");
                var nodeLink = pairedContainer.QuerySelector("span.iconbox > a");

                if (nodeLink == null)
                    continue;

                var item = CreatePairedNodeLinks(filters, nodeTitle, nodeLink, "column 1", count++, feedUrl, false);

                // Unfortunately the reference site links are included in the column links, so the
                // AMERICAN THINKER link signals the end of the article list in column 1
                if (item.FeedAttributes.Url.Contains("americanthinker.com"))
                    break;

                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    //TryParseEmbeddedUrl(item, _selectors);
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 2
        container = document.QuerySelector("#column_2");
        if (container != null)
        {
            var pairedContainers = container.QuerySelectorAll("ul > li > h2");
            count = 1;
            foreach (var pairedContainer in pairedContainers)
            {
                var nodeTitle = pairedContainer.QuerySelector("span.mf-headline > a");
                var nodeLink = pairedContainer.QuerySelector("span.iconbox > a");

                if (nodeLink == null)
                    continue;

                var item = CreatePairedNodeLinks(filters, nodeTitle, nodeLink, "column 2", count++, feedUrl, false);

                // Unfortunately the reference site links are included in the column links, so the
                // CINDY ADAMS link signals the end of the article list in column 2
                if (item.FeedAttributes.Url.Contains("cindy-adams"))
                    break;

                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    //TryParseEmbeddedUrl(item, _selectors);
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 3
        container = document.QuerySelector("#column_3");
        if (container != null)
        {
            var pairedContainers = container.QuerySelectorAll("ul > li > h2");
            count = 1;
            foreach (var pairedContainer in pairedContainers)
            {
                var nodeTitle = pairedContainer.QuerySelector("span.mf-headline > a");
                var nodeLink = pairedContainer.QuerySelector("span.iconbox > a");

                if (nodeLink == null)
                    continue;

                var item = CreatePairedNodeLinks(filters, nodeTitle, nodeLink, "column 3", count++, feedUrl, false);

                // Unfortunately the reference site links are included in the column links, so the
                // PRIVACY POLICY link signals the end of the article list in column 2
                if (item.FeedAttributes.Url.Contains("privacy-policy-2"))
                    break;

                if (item != null && !item.FeedAttributes.Url.Contains("#the-comments") && !item.FeedAttributes.Url.Contains("#comment-"))
                {
                    //TryParseEmbeddedUrl(item, _selectors);
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        return list;
    }
}