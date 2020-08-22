using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using RssFeeder.Models;
using RssFeeder.Mvc.Models;
using Serilog;

namespace RssFeeder.Mvc.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class RssController : ControllerBase
    {
        private readonly IRepository<RssFeederRepository> _repo;
        private readonly IMemoryCache _cache;
        private readonly IEnumerable<string> _collectionList = new string[] { "drudge-report", "eagle-slant" };
        private readonly string _sourceFile = "feeds.json";
        private readonly List<FeedModel> _feeds;

        public RssController(IRepository<RssFeederRepository> repository, IMemoryCache cache)
        {
            _repo = repository;
            _cache = cache;
            _feeds = System.Text.Json.JsonSerializer.Deserialize<List<FeedModel>>(
                System.IO.File.ReadAllText(_sourceFile),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        [HttpGet, HttpHead, Route("{id}"), ResponseCache(Duration = 60 * 60), Produces("text/xml")]
        public async Task<IActionResult> Get(string id)
        {
            // FIXME: Hack until I can find a better way to handle this
            if (!_collectionList.Contains(id.ToLowerInvariant()))
            {
                return NotFound();
            }

            // Create document with incoming parameter values
            var agent = new Agent
            {
                BrowserAgent = HttpContext.Request.Headers[HeaderNames.UserAgent].ToString() ?? "",
                IpAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                Referrer = HttpContext.Request.Headers[HeaderNames.Referer].ToString() ?? "",
                Timestamp = DateTime.Now
            };

            Log.Information("Detected user info: {@UserAgent}", agent);

            string s = await GetSyndicationItems(id);

            if (Request.Method.Equals("HEAD"))
            {
                Response.ContentLength = s.Length;
                return Ok();
            }
            else
            {
                return new ContentResult
                {
                    Content = s.ToString(),
                    ContentType = "text/xml",
                    StatusCode = 200
                };
            }
        }

        private async Task<string> GetSyndicationItems(string id)
        {
            // See if we already have the items in the cache
            if (_cache.TryGetValue($"{id}_items", out string s))
            {
                return s;
            }

            var sb = new StringBuilder();
            var stringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
            int days = (id.ToLowerInvariant() == "drudge-report" ? 3 : 2);

            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Async = true, Indent = true, Encoding = Encoding.UTF8 }))
            {
                var rssWriter = new AtomFeedWriter(xmlWriter);

                var feed = GetFeed(id);
                await rssWriter.WriteTitle(feed.title);
                await rssWriter.Write(new SyndicationLink(new Uri(feed.url)));
                await rssWriter.WriteUpdated(DateTimeOffset.UtcNow);

                // Add Items
                foreach (var item in await GetFeedItems(id.ToLowerInvariant(), days))
                {
                    var si = new SyndicationItem()
                    {
                        Id = item.Id,
                        Title = item.Title.Replace("\u0008", ""),
                        Description = item.Description.Replace("\u0008", ""),
                        Published = item.DateAdded,
                        LastUpdated = item.DateAdded
                    };

                    si.AddLink(new SyndicationLink(new Uri(item.Url)));
                    si.AddContributor(new SyndicationPerson(item.SiteName, feed.authoremail, AtomContributorTypes.Author));

                    await rssWriter.Write(si);
                }

                xmlWriter.Flush();
            }

            // Add the items to the cache before returning
            s = stringWriter.ToString();
            _cache.Set<string>($"{id}_items", s, TimeSpan.FromMinutes(60));

            return s;
        }

        private FeedModel GetFeed(string id)
        {
            return _feeds.FirstOrDefault(q => q.collectionname == id);
        }

        private async Task<IEnumerable<RssFeedItem>> GetFeedItems(string id, int days)
        {
            _repo.Init("drudge-report");
            Log.Information("Retrieving {days} days items for '{id}'", days, id);
            
            var _items = await _repo.GetItemsAsync<RssFeedItem>(q => q.FeedId == id);

            return _items
                .Where(q => q.DateAdded >= DateTime.Now.Date.AddDays(-days))
                .OrderByDescending(q => q.DateAdded);
        }
    }
}
