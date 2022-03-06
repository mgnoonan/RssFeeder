namespace RssFeeder.Console.FeedBuilders;

internal class ConservagatorFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public ConservagatorFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) : base(logger, webUtilities, utilities)
    {
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
        bool offset = DateTime.Now.Hour % 2 == 0;
        int count;

        // Load and parse the html from the source file
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Articles are grouped in columns by news site
        var containers = document.QuerySelectorAll("div.wp-block-column");
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
        int sectionCounter = offset ? 0 : 12;

        foreach (var element in containers.Skip(sectionCounter).Take(12))
        {
            var nodes = element.QuerySelectorAll("ul.rss-aggregator > li.feed-item > a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, $"{sectionName[sectionCounter]} section", count++, feedUrl);
                    if (item != null)
                    {
                        log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }

            sectionCounter++;
        }

        return list;
    }
}
