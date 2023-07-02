namespace RssFeeder.Console.FeedBuilders;

internal class ConservagatorFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public ConservagatorFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities, IUnlaunchClient unlaunchClient) : base(logger, webUtilities, utilities, unlaunchClient)
    {
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
        Initialize(feedUrl, feedFilters, html);
        var items = GenerateRssFeedItemList(html);
        PostProcessing(feedCollectionName, feedUrl, items);

        return items;
    }

    public List<RssFeedItem> GenerateRssFeedItemList(string html)
    {
        var list = new List<RssFeedItem>();
        int count;

        // Articles are grouped in columns by news site
        var containers = _document.QuerySelectorAll("div.wp-block-column");
        string[] sectionName = new string[]
        {
            "Fox News", "New York Post", "Breitbart",
            "ZeroHedge", "Western Journal", "Daily Wire",
            "Washington Examiner","Gateway Pundit","Townhall",
            "Epoch Times","Daily Caller","Newsmax",
            "PJ Media","Red State","National Review",
            "The Blaze","Washington Times","InfoWars",
            "Hot Air","Twitchy","American Thinker",
            "Federalist","Conservative Treehouse","WND",
            "OAN","Powerline","Rush Limbaugh",
            "Newsbusters","BPR","Wayne Dupree",
            "Bearing Arms","American Conservative","Unz Review",
            "Washington Free Beacon","Trending Views","American Greatness",
            "CNS News","Daily Signal","Judicial Watch",
            "Right Scoop","True Pundit","Louder with Crowder",
            "National File","Todd Starnes","Gatestone Institute",
            "American Spectator","Summit News","LifeZette",
            "Liberty Loft","Patriot Post","Conservative Review",
            "Trending Politics","IOTW Report","Conservative Firing Line",
            "Strange Sounds","CDN","Moonbattery",
            "Right Wire Report","GenZ Conservative","America Can We Talk",
            "Pundit Beacon","Blue State Conservative","CommDigiNews",
            "Freedom First Press"
        };
        int sectionCounter = 0;

        foreach (var element in containers)
        {
            var nodes = element.QuerySelectorAll("ul.rss-aggregator > li.feed-item > a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes.Take(1))
                {
                    var item = CreateNodeLinks(_feedFilters, node, $"{sectionName[sectionCounter]} section", count++, _feedUrl, false);
                    if (item != null)
                    {
                        _log.Write(_logLevel, "FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }

            sectionCounter++;
        }

        return list;
    }
}
