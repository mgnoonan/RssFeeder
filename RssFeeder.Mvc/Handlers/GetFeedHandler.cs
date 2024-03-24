namespace RssFeeder.Mvc.Handlers;

public class GetFeedHandler : IRequestHandler<GetFeedQuery, string>
{
    private readonly ICacheStack<IDatabaseService> _cacheStack;
    private readonly List<FeedModel> _feeds;
    private readonly ILogger _log;
    private readonly string _sourceFile = "feeds.json";

    public GetFeedHandler(ICacheStack<IDatabaseService> cacheStack, ILogger log)
    {
        _cacheStack = cacheStack;
        _feeds = System.Text.Json.JsonSerializer.Deserialize<List<FeedModel>>(
                System.IO.File.ReadAllText(_sourceFile),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        _log = log;
    }

    public Task<string> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        _log.Information("Start request for feed id {id}", request.Id);
        _log.Information("Detected user info: {@UserAgent}", request.Agent);

        if (!FeedExists(request.Id.ToLowerInvariant()))
        {
            _log.Warning("Invalid feed id {id}", request.Id);
            return Task.FromResult(string.Empty);
        }

        return GetSyndicationItems(request.Id, request.Agent.BrowserAgent, request.Agent.QueryString);
    }

    private async Task<string> GetSyndicationItems(string id, string userAgent, QueryString queryString)
    {
        bool textOnly = userAgent.Contains("Feedly/1.0", StringComparison.InvariantCultureIgnoreCase) || !queryString.HasValue;
        string key = string.Concat(id, "_feed_", textOnly ? "TextOnly" : "Xml");

        // See if we already have the items in the cache
        string xml = await _cacheStack.GetOrSetAsync<string>(key, async (old, context) =>
        {
            int days = 5;
            _log.Information("CACHE MISS {key}: Loading feed {id} with items for {days} days ", key, id, days);

            var items = await context.GetItemsAsync(new QueryDefinition("SELECT * FROM c WHERE c.FeedId = @id")
                .WithParameter("@id", id));

            return await FormatItemsAsAtomXml(
                id,
                items.Where(q => q.DateAdded >= DateTime.Now.Date.AddDays(-days)).OrderByDescending(q => q.DateAdded),
                textOnly
                );
        }, new CacheSettings(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(15)));

        return xml;
    }

    private async Task<string> FormatItemsAsAtomXml(string id, IEnumerable<RssFeedItem> items, bool textOnly)
    {
        var sb = new StringBuilder();
        var stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
        var feed = GetFeed(id);

        using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Async = true, Indent = true, Encoding = Encoding.UTF8 }))
        {
            var rssWriter = new AtomFeedWriter(xmlWriter);

            await rssWriter.WriteTitle(feed.title);
            await rssWriter.Write(new SyndicationLink(new Uri(feed.url)));
            await rssWriter.WriteUpdated(DateTimeOffset.UtcNow);

            // Add Items
            foreach (var item in items)
            {
                string text = textOnly ? System.Web.HttpUtility.HtmlDecode(item.Description) : item.ArticleText;
                try
                {
                    var si = new SyndicationItem()
                    {
                        Id = item.Id,
                        Title = Regex.Replace(item.Title, "[\u0001-\u001f\ufffe]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250)),
                        Description = Regex.Replace(text ?? string.Empty, "[\u0001-\u001f\ufffe]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250)),
                        Published = item.DateAdded,
                        LastUpdated = item.DateAdded
                    };

                    si.AddLink(new SyndicationLink(new Uri(item.Url)));
                    si.AddContributor(
                        new SyndicationPerson(
                            string.IsNullOrWhiteSpace(item.SiteName) ? string.IsNullOrWhiteSpace(item.HostName) ? "Name Unavailable" : item.HostName : item.SiteName,
                            feed.authoremail,
                            AtomContributorTypes.Author));

                    await rssWriter.Write(si);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error building item {urlHash}:{url}", item.UrlHash, item.Url);
                }
            }

            xmlWriter.Flush();
        }

        // Add the items to the cache before returning
        return stringWriter.ToString();
    }

    private FeedModel GetFeed(string id)
    {
        return _feeds.FirstOrDefault(q => q.collectionname == id);
    }

    private bool FeedExists(string id)
    {
        return _feeds.Any(q => q.collectionname == id);
    }
}
