﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using RssFeeder.Models;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class RssController : ControllerBase
    {
        private readonly IRepository<RssFeederRepository> _repo;
        private readonly IMemoryCache _cache;
        private readonly string _collectionID = "drudge-report";

        public RssController(IRepository<RssFeederRepository> repository, IMemoryCache cache)
        {
            _repo = repository;
            _cache = cache;
        }

        private IEnumerable<RssFeedItem> GetFeedItems(string id)
        {
            return _cache.GetOrCreate<IEnumerable<RssFeedItem>>(id, entry =>
            {
                entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(60));
                _repo.Init(id);

                var _items = _repo.GetAllDocuments<RssFeedItem>("rssfeeder", id);

                return _items
                    .Where(q => q.DateAdded >= DateTime.Now.Date.AddDays(-3))
                    .OrderByDescending(q => q.DateAdded);
            });
        }

        [HttpGet, Route("{id}"), Produces("application/rss+xml")]
        public async Task<IActionResult> Get(string id)
        {
            // Hack until I can find a better way to handle this
            if (id.ToLowerInvariant() != _collectionID)
            {
                return NotFound();
            }

            string s = await GetSyndicationItems(id);

            return new ContentResult
            {
                Content = s.ToString(),
                ContentType = "text/xml",
                StatusCode = 200
            };
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

            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Async = true, Indent = true, Encoding = Encoding.UTF8 }))
            {
                var rssWriter = new AtomFeedWriter(xmlWriter);

                await rssWriter.WriteTitle("The Drudge Report");
                await rssWriter.Write(new SyndicationLink(new Uri("https://www.drudgereport.com/")));
                await rssWriter.WriteUpdated(DateTimeOffset.UtcNow);

                // Add Items
                foreach (var item in GetFeedItems(id.ToLowerInvariant()).OrderByDescending(i => i.DateAdded))
                {
                    var si = new SyndicationItem()
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Description = item.Description,
                        Published = item.DateAdded,
                        LastUpdated = item.DateAdded
                    };

                    si.AddLink(new SyndicationLink(new Uri(item.Url)));
                    si.AddContributor(new SyndicationPerson(item.SiteName, "drudge@drudgereport.com", AtomContributorTypes.Author));

                    await rssWriter.Write(si);
                }

                xmlWriter.Flush();
            }

            // Add the items to the cache before returning
            s = stringWriter.ToString();
            _cache.Set<string>($"{id}_items", s, TimeSpan.FromMinutes(60));

            return s;
        }
    }
}
