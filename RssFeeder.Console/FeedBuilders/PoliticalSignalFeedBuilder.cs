namespace RssFeeder.Console.FeedBuilders;

internal class PoliticalSignalFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public PoliticalSignalFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        // #featured_post_1 > h2 > a
        var containers = document.QuerySelectorAll("#home_page_featured");
        string location = "main headlines";
        int articleCount = GetArticlesBySection(filters, feedUrl, list, containers, location, "li > h2 > a");
        _log.Information("{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        // Column 1 section
        containers = document.QuerySelectorAll("#column_1");
        location = "column 1";
        articleCount = GetArticlesBySection(filters, feedUrl, list, containers, location, "a");
        _log.Information("{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        // Column 2 section
        containers = document.QuerySelectorAll("#column_2");
        location = "column 2";
        articleCount = GetArticlesBySection(filters, feedUrl, list, containers, location, "a");
        _log.Information("{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        // Column 3 section
        containers = document.QuerySelectorAll("#column_3");
        location = "column 3";
        articleCount = GetArticlesBySection(filters, feedUrl, list, containers, location, "a");
        _log.Information("{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        return list;
    }

    private int GetArticlesBySection(List<string> filters, string feedUrl, List<RssFeedItem> fullList, IHtmlCollection<IElement> containers, string location, string querySelector, bool isHeadline = false)
    {
        if (containers == null)
        {
            return 0;
        }

        var list = new List<RssFeedItem>();

        foreach (var c in containers)
        {
            var nodes = c.QuerySelectorAll(querySelector);
            if (nodes?.Length > 0)
            {
                int count = 1;
                string text = "";

                foreach (var node in nodes)
                {
                    if (String.Join(' ', node.ParentElement.ClassList).Contains("mf-headline"))
                    {
                        text = node.Text();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(text))
                        {
                            node.TextContent = text;
                            text = "";
                        }

                        var item = CreateNodeLinks(filters, node, location, count, feedUrl, isHeadline);
                        if (item != null)
                        {
                            _log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                            list.Add(item);
                        }

                        count++;
                    }
                }
            }
        }

        fullList.AddRange(list);
        return list.Count;
    }
}
