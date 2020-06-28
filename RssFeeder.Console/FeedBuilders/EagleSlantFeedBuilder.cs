using System.Collections.Generic;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class EagleSlantFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public EagleSlantFeedBuilder(ILogger log) : base(log)
        { }

        public List<RssFeedItem> ParseRssFeedItems(ILogger log, RssFeed feed, string html)
        {
            var filters = feed.Filters ?? new List<string>();

            var items = ParseRssFeedItems(log, html, filters);
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

        public List<RssFeedItem> ParseRssFeedItems(ILogger log, string html, List<string> filters)
        {
            var list = new List<RssFeedItem>();
            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));

            // Centered main headline(s)

            var nodes = doc.DocumentNode.SelectNodes("//div[@id=\"featured\"]/div/h2/a");
            int count;
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(log, filters, node, "main headline", count++);
                    if (item != null)
                    {
                        log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            // Left column articles

            nodes = doc.DocumentNode.SelectNodes("//div[@id=\"column-1\"]/div/div/ul/li/a");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(log, filters, node, "left column", count++);
                    if (item != null)
                    {
                        log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }


            // Middle column articles

            nodes = doc.DocumentNode.SelectNodes("//div[@id=\"column-2\"]/div/div/ul/li/a");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(log, filters, node, "left column", count++);
                    if (item != null)
                    {
                        log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}
