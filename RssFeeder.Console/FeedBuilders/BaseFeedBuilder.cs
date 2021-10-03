using System;
using System.Collections.Generic;
using System.Net;
using AngleSharp.Dom;
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

        protected RssFeedItem CreateNodeLinks(List<string> filters, IElement node, string location, int count)
        {
            string title = WebUtility.HtmlDecode(node.Text().Trim());

            // Replace all errant spaces, which sometimes creep into Drudge's URLs
            var attr = node.Attributes.GetNamedItem("href");
            string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

            return CreateNodeLinks(filters, location, count, title, ref linkUrl);
        }

        protected RssFeedItem CreateNodeLinks(List<string> filters, HtmlNode node, string location, int count)
        {
            string title = WebUtility.HtmlDecode(node.InnerText.Trim());

            try
            {
                HtmlAttribute attr = node.Attributes["href"];

                // Replace all errant spaces, which sometimes creep into Drudge's URLs
                string linkUrl = attr.Value.Trim().Replace(" ", string.Empty);

                return CreateNodeLinks(filters, location, count, title, ref linkUrl);
            }
            catch (NullReferenceException)
            {
                log.Warning("Unable to resolve reference for location '{location}':'{title}'", location, title);
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error encountered reading location '{location}':{count}", location, count);
            }

            return null;
        }

        private RssFeedItem CreateNodeLinks(List<string> filters, string location, int count, string title, ref string linkUrl)
        {
            // Sometimes Drudge has completely empty links, ignore them
            if (string.IsNullOrEmpty(linkUrl))
            {
                return null;
            }

            // Repair any protocol typos if possible
            if (!linkUrl.ToLower().StartsWith("http"))
            {
                linkUrl = webUtils.RepairUrl(linkUrl);
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
                    LinkLocation = $"{location}, article {count}",
                    FeedAttributes = new FeedAttributes()
                    {
                        Title = WebUtility.HtmlDecode(title),
                        Url = linkUrl,
                        UrlHash = hash,
                        DateAdded = DateTime.Now.ToUniversalTime(),
                        LinkLocation = $"{location}, article {count}"
                    }
                };
            }

            return null;
        }
    }
}
