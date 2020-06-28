using System;
using System.Collections.Generic;
using System.Net;
using HtmlAgilityPack;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class BaseFeedBuilder
    {
        private readonly ILogger log;

        public BaseFeedBuilder(ILogger logger)
        {
            log = logger;
        }

        protected RssFeedItem CreateNodeLinks(ILogger log, List<string> filters, HtmlNode node, string location, int count)
        {
            string title = WebUtility.HtmlDecode(node.InnerText.Trim());

            // Replace all errant spaces, which sometimes creep into Drudge's URLs
            HtmlAttribute attr = node.Attributes["href"];
            string linkUrl = attr.Value.Trim().Replace(" ", string.Empty).ToLower();

            // Repair any protocol typos if possible
            if (!linkUrl.StartsWith("http"))
            {
                log.Information("Attempting to repair link '{url}'", linkUrl);
                linkUrl = WebTools.RepairUrl(linkUrl);
                log.Information("Repaired link '{url}'", linkUrl);
            }

            // Calculate the MD5 hash for the link so we can be sure of uniqueness
            string hash = Utility.Utility.CreateMD5Hash(linkUrl);
            if (filters.Contains(hash))
            {
                log.Debug("Hash '{hash}' found in filter list", hash);
                return null;
            }

            if (linkUrl.Length > 0 && title.Length > 0)
            {
                return new RssFeedItem()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = WebUtility.HtmlDecode(title),
                    Url = linkUrl,
                    UrlHash = hash,
                    DateAdded = DateTime.Now.ToUniversalTime(),
                    LinkLocation = $"{location}, article {count}"
                };
            }

            return null;
        }
    }
}
