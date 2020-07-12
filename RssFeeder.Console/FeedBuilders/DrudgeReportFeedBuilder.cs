using HtmlAgilityPack;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace RssFeeder.Console.FeedBuilders
{
    class DrudgeReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public DrudgeReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
        { }

        public List<RssFeedItem> ParseRssFeedItems(RssFeed feed, string html)
        {
            var filters = feed.Filters ?? new List<string>();

            var items = ParseRssFeedItems(html, filters);
            log.Information("FOUND {count} articles in {url}", items.Count, feed.Url);

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

        public List<RssFeedItem> ParseRssFeedItems(string html, List<string> filters)
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
                    var item = CreateNodeLinks(filters, node, "main headline", count++);
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
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(filters, node, "above the fold", count++);
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
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(filters, node, "left column", count++);
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
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(filters, node, "middle column", count++);
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
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                    {
                        var item = CreateNodeLinks(filters, node, "right column", count++);
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
    }
}
