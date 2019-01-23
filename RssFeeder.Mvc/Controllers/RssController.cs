using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using RssFeeder.Models;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class RssController : ControllerBase
    {
        private IRepository<RssFeederRepository> _repo;
        private IMemoryCache _cache;
        private IEnumerable<RssFeedItem> _items;
        //private readonly string _collectionID = "drudge-report";

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
                _items = _repo.GetAllDocuments<RssFeedItem>("rssfeeder", id);
                return _items;
            });
        }

        [HttpGet, Route("{id}"), Produces("text/xml")]
        public async Task<IActionResult> Get(string id)
        {
            // Hack until I can find a better way to handle this
            if (id.ToLowerInvariant() != "drudge-report")
            {
                return NotFound();
            }

            var sw = new StringWriter();
            using (XmlWriter xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true, Indent = true }))
            {
                var writer = new RssFeedWriter(xmlWriter);

                //
                // Add Title
                await writer.WriteTitle("The Drudge Report");

                //
                // Add Description
                await writer.WriteDescription("The Drudge Report");

                //
                // Add Link
                await writer.Write(new SyndicationLink(new Uri("https://www.drudgereport.com/")));

                //
                // Add managing editor
                //await writer.Write(new SyndicationPerson("managingeditor", "managingeditor@contoso.com", RssContributorTypes.ManagingEditor));

                //
                // Add publish date
                await writer.WritePubDate(DateTimeOffset.UtcNow);

                //
                // Add custom element
                //var customElement = new SyndicationContent("customElement");

                //customElement.AddAttribute(new SyndicationAttribute("attr1", "true"));
                //customElement.AddField(new SyndicationContent("Company", "Contoso"));

                //await writer.Write(customElement);

                //
                // Add Items
                foreach (var item in GetFeedItems(id.ToLowerInvariant()).OrderByDescending(i => i.DateAdded))
                {
                    var si = new SyndicationItem()
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Description = item.Description,
                        Published = item.DateAdded
                    };

                    si.AddLink(new SyndicationLink(new Uri(item.Url)));
                    //si.AddCategory(new SyndicationCategory("Technology"));
                    //si.AddContributor(new SyndicationPerson(null, "user@contoso.com", RssContributorTypes.Author));

                    await writer.Write(si);
                }

                //
                // Done
                xmlWriter.Flush();
            }

            //return Ok(sw.ToString());
            return new ContentResult
            {
                Content = sw.ToString(),
                ContentType = "text/xml",
                StatusCode = 200
            };
        }
    }
}
