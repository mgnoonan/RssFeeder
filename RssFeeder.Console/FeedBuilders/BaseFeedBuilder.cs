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
        protected readonly ILogger log;
        readonly IWebUtils webUtils;
        readonly IUtils utils;

        public BaseFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities)
        {
            log = logger;
            webUtils = webUtilities;
            utils = utilities;
        }

        protected RssFeedItem CreateNodeLinks(List<string> filters, HtmlNode node, string location, int count)
        {
            string title = WebUtility.HtmlDecode(node.InnerText.Trim());

            // Replace all errant spaces, which sometimes creep into Drudge's URLs
            HtmlAttribute attr = node.Attributes["href"];
            string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

            // Sometimes Drudge has completely empty links, ignore them
            if (string.IsNullOrEmpty(linkUrl))
            {
                return null;
            }

            // Repair any protocol typos if possible
            if (!linkUrl.ToLower().StartsWith("http"))
            {
                log.Information("Attempting to repair link '{url}'", linkUrl);
                linkUrl = webUtils.RepairUrl(linkUrl);
                log.Information("Repaired link '{url}'", linkUrl);
            }

            // Calculate the MD5 hash for the link so we can be sure of uniqueness
            string hash = utils.CreateMD5Hash(linkUrl.ToLower());
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
