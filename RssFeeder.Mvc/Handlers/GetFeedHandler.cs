using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using RssFeeder.Mvc.Models;
using RssFeeder.Mvc.Queries;
using RssFeeder.Mvc.Services;
using Serilog;

namespace RssFeeder.Mvc.Handlers;

public class GetFeedHandler : IRequestHandler<GetFeedQuery, string>
{
    private readonly IDatabaseService _repo;
    private readonly IMemoryCache _cache;
    private readonly List<FeedModel> _feeds;
    private readonly string _sourceFile = "feeds.json";

    public GetFeedHandler(IDatabaseService repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
        _feeds = System.Text.Json.JsonSerializer.Deserialize<List<FeedModel>>(
                System.IO.File.ReadAllText(_sourceFile),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FeedModel>();
    }

    public Task<string> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        Log.Information("Start request for feed id {id}", request.Id);
        Log.Information("Detected user info: {@UserAgent}", request.Agent);

        if (!FeedExists(request.Id.ToLowerInvariant()))
        {
            Log.Warning("Invalid feed id {id}", request.Id);
            return Task.FromResult(string.Empty);
        }

        return GetSyndicationItems(request.Id);
    }

    private async Task<string> GetSyndicationItems(string id)
    {
        // See if we already have the items in the cache
        if (_cache.TryGetValue($"{id}_items", out string? s))
        {
            Log.Information("CACHE HIT: Returning {bytes} bytes", s?.Length ?? 0);
            return s;
        }

        Log.Information("CACHE MISS: Loading feed items for {id}", id);
        var sb = new StringBuilder();
        var stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
        int days = 5;
        var feed = GetFeed(id);

        using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Async = true, Indent = true, Encoding = Encoding.UTF8 }))
        {
            var rssWriter = new AtomFeedWriter(xmlWriter);

            await rssWriter.WriteTitle(feed.title);
            await rssWriter.Write(new SyndicationLink(new Uri(feed.url)));
            await rssWriter.WriteUpdated(DateTimeOffset.UtcNow);

            // Add Items
            foreach (var item in await GetFeedItems(id.ToLowerInvariant(), days))
            {
                try
                {
                    var si = new SyndicationItem()
                    {
                        Id = item.Id,
                        Title = Regex.Replace(item.Title, "[\u0001-\u001f]", ""),
                        Description = Regex.Replace(item.ArticleText, "[\u0001-\u001f]", ""),
                        Published = item.DateAdded,
                        LastUpdated = item.DateAdded
                    };

                    si.AddLink(new SyndicationLink(new Uri(item.Url)));
                    si.AddContributor(new SyndicationPerson(string.IsNullOrWhiteSpace(item.SiteName) ? item.HostName : item.SiteName, feed.authoremail, AtomContributorTypes.Author));

                    await rssWriter.Write(si);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error building item {urlHash}:{url}", item.UrlHash, item.Url);
                }
            }

            xmlWriter.Flush();
        }

        // Add the items to the cache before returning
        s = stringWriter.ToString();
        _cache.Set<string>($"{id}_items", s, TimeSpan.FromMinutes(60));
        Log.Information("CACHE SET: Storing feed items for {id} for {minutes} minutes", id, 60);

        return s;
    }

    private FeedModel? GetFeed(string id)
    {
        return _feeds.FirstOrDefault(q => q.collectionname == id);
    }

    private bool FeedExists(string id)
    {
        return _feeds.Any(q => q.collectionname == id);
    }

    private async Task<IEnumerable<RssFeedItem>> GetFeedItems(string id, int days)
    {
        Log.Information("Retrieving {days} days items for '{id}'", days, id);

        var _items = await _repo.GetItemsAsync(new Microsoft.Azure.Cosmos.QueryDefinition("SELECT * FROM c WHERE c.FeedId = @id").WithParameter("@id", id));

        return _items
            .Where(q => q.DateAdded >= DateTime.Now.Date.AddDays(-days))
            .OrderByDescending(q => q.DateAdded);
    }
}
