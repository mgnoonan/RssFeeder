using System.Collections.Generic;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class RantinglyFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public RantinglyFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
        { }

        public List<RssFeedItem> ParseRssFeedItems(RssFeed feed, string html)
        {
            var filters = feed.Filters ?? new List<string>();

            var items = ParseRssFeedItems(html, filters);
            log.Information("FOUND {count} articles in {url}", items.Count, feed.Url);

            // Replace any relative paths and add the feed id
            foreach (var item in items)
            {
                item.FeedId = feed.CollectionName;
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

            // Above the fold headline(s)
            var nodes = doc.DocumentNode.SelectNodes("#wrapper > ul > li > a");
            int count;
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(filters, node, "above the fold", count++);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            //// Featured headline(s)
            //nodes = doc.DocumentNode.SelectNodes("#content-wrap > div.page-header > div > a");
            //if (nodes != null)
            //{
            //    count = 1;
            //    foreach (HtmlNode node in nodes)
            //    {
            //        string title = WebUtility.HtmlDecode(node.InnerText.Trim());

            //        var item = CreateNodeLinks(filters, node, "main headlines", count++);
            //        if (item != null)
            //        {
            //            log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
            //            list.Add(item);
            //        }
            //    }
            //}

            return list;
        }
    }
}
