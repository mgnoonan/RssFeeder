namespace RssFeeder.Console.FeedBuilders;

internal class PoliticalSignalFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    private readonly IUnlaunchClient _unlaunchClient;

    public PoliticalSignalFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(log, webUtilities, utilities)
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

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Main Headlines section
        // #featured_post_1 > h2 > a
        var containers = document.QuerySelectorAll("#home_page_featured");
        string location = "main headlines";
        int articleCount = GetArticlesBySection(list, containers, location, "li > h2 > a");
        _log.Write(_logLevel, "{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        // Column 1 section
        containers = document.QuerySelectorAll("#column_1");
        location = "column 1";
        articleCount = GetArticlesBySection(list, containers, location, "a");
        _log.Write(_logLevel, "{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        // Column 2 section
        containers = document.QuerySelectorAll("#column_2");
        location = "column 2";
        articleCount = GetArticlesBySection(list, containers, location, "a");
        _log.Write(_logLevel, "{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        // Column 3 section
        containers = document.QuerySelectorAll("#column_3");
        location = "column 3";
        articleCount = GetArticlesBySection(list, containers, location, "a");
        _log.Write(_logLevel, "{location}: {sectionCount} sections, {articleCount} articles", location, containers.Length, articleCount);

        return list;
    }

    private int GetArticlesBySection(List<RssFeedItem> fullList, IHtmlCollection<IElement> containers, string location, string querySelector, bool isHeadline = false)
    {
        if (containers == null)
        {
            return 0;
        }

        var list = new List<RssFeedItem>();
        int count = 1;

        foreach (var c in containers)
        {
            var nodes = c.QuerySelectorAll(querySelector);
            if (nodes?.Length > 0)
            {
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

                        var item = CreateNodeLinks(_feedFilters, node, location, count, _feedUrl, isHeadline);
                        if (item != null)
                        {
                            _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
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
