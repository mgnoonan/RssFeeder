using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class DrudgeReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        private readonly string collectionName = "drudge-report";

        public DrudgeReportFeedBuilder(ILogger log) : base(log)
        { }

        public List<RssFeedItem> ParseRssFeedItems(ILogger log, RssFeed feed, out string html)
        {
            var filters = feed.Filters ?? new List<string>();

            html = WebTools.GetUrl(feed.Url);

            var items = ParseRssFeedItems(log, html, filters);

            // Replace any relative paths and add the feed id
            foreach (var item in items)
            {
                item.FeedId = feed.Id;
                if (item.Url.StartsWith("/"))
                {
                    item.Url = feed.Url + item.Url;
                }
            }

            return items;
        }

        public List<RssFeedItem> ParseRssFeedItems(ILogger log, string html, List<string> filters)
        {
            var list = new List<RssFeedItem>();

            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));

            // Centered main headline(s)

            var nodes = doc.DocumentNode.SelectNodes("//center");
            int count = 1;
            foreach (HtmlNode link in nodes)
            {
                if (!link.InnerHtml.Contains("MAIN HEADLINE"))
                {
                    continue;
                }

                var nodeList = link.Descendants("a").ToList();
                foreach (var node in nodeList)
                {
                    var item = CreateNodeLinks(log, filters, node, "main headline", count++);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }

                // Get out of the loop, there are no more headlines
                break;
            }


            // Above the fold top headlines

            nodes = doc.DocumentNode.SelectNodes("/html/body/tt/b/tt/b/a[@href]");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = HttpUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(log, filters, node, "above the fold", count++);
                        if (item != null)
                        {
                            log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                            list.Add(item);
                        }
                    }
                }
            }


            // Left column articles

            nodes = doc.DocumentNode.SelectNodes("//table/tr/td[1]/tt/b/a[@href]");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = HttpUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(log, filters, node, "left column", count++);
                        if (item != null)
                        {
                            log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                            list.Add(item);
                        }
                    }
                }
            }


            // Middle column articles

            nodes = doc.DocumentNode.SelectNodes("//table/tr/td[3]/tt/b/a[@href]");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = HttpUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(log, filters, node, "middle column", count++);
                        if (item != null)
                        {
                            log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                            list.Add(item);
                        }
                    }
                }
            }


            // Right column articles

            nodes = doc.DocumentNode.SelectNodes("//table/tr/td[5]/tt/b/a[@href]");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = HttpUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(log, filters, node, "right column", count++);
                        if (item != null)
                        {
                            log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                            list.Add(item);
                        }
                    }
                }
            }

            return list;
        }

        private RssFeedItem CreateNodeLinks(ILogger log, List<string> filters, HtmlNode node, string location, int count)
        {
            string title = HttpUtility.HtmlDecode(node.InnerText.Trim());

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
                    Title = HttpUtility.HtmlDecode(title),
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
