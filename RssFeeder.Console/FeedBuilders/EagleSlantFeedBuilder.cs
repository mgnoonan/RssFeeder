using System.Collections.Generic;
using System.IO;
using System.Web;
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
            int count = 1;

            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));

            // Centered main headline(s)
            // TODO: I've seen it but need to wait
            // until the next occurrence is captured.


            // Left column articles

            var nodes = doc.DocumentNode.SelectNodes("//div[@id=\"column-1\"]/div/div/ul/li/a");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = HttpUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(log, filters, node, "left column", count++);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
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
                    string title = HttpUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(log, filters, node, "left column", count++);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}
