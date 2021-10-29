using System.Collections.Generic;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class EagleSlantFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public EagleSlantFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
        { }

        public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
        {
            return GenerateRssFeedItemList(feed.CollectionName, feed.Url, feed.Filters, html);
        }

        public List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
        {
            var items = GenerateRssFeedItemList(html, feedFilters ?? new List<string>());
            PostProcessing(feedCollectionName, feedUrl, items);

            return items;
        }

        public List<RssFeedItem> GenerateRssFeedItemList(string html, List<string> filters)
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

                    var item = CreateNodeLinks(filters, node, "main headlines", count++);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
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

                    var item = CreateNodeLinks(filters, node, "left column", count++);
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
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(filters, node, "middle column", count++);
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
