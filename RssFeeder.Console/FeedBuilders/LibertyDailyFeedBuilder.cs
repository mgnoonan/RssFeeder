using System.Collections.Generic;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class LibertyDailyFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public LibertyDailyFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
        { }

        public List<RssFeedItem> ParseRssFeedItems(RssFeed feed, string html)
        {
            return ParseRssFeedItems(feed.CollectionName, feed.Url, feed.Filters, html);
        }

        public List<RssFeedItem> ParseRssFeedItems(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
        {
            var items = ParseRssFeedItems(html, feedFilters ?? new List<string>());
            log.Information("FOUND {count} articles in {url}", items.Count, feedUrl);

            // Replace any relative paths and add the feed id
            foreach (var item in items)
            {
                item.FeedId = feedCollectionName;
                item.FeedAttributes.FeedId = feedCollectionName;

                if (item.Url.StartsWith("/"))
                {
                    item.Url = feedUrl + item.Url;
                    item.FeedAttributes.Url = feedUrl + item.Url;
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
            // //div[@class=\"drudgery-top-links\"]/div/a
            var nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-top-links\"]/div/a");
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

            // Featured headline(s)
            // //div[@class=\"drudgery-featured\"]/div/a
            nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-featured\"]/div/a");
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

            // Column 1 Articles
            // //div[@class=\"drudgery-column-1\"]/div[@class=\"drudgery-articles\"]/div/a
            // #main > div.drudgery-column-1 > div:nth-child(2) > div:nth-child(1) > a
            nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-1\"]/div[@class=\"drudgery-articles\"]/div/a");
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

            // Column 2 Articles
            // //div[@class=\"drudgery-column-2\"]/div[@class=\"drudgery-articles\"]/div/a
            // #main > div.drudgery-column-2 > div:nth-child(2) > div:nth-child(1) > a
            nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-2\"]/div[@class=\"drudgery-articles\"]/div/a");
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

            // Column 3 Articles
            // //div[@class=\"drudgery-column-3\"]/div[@class=\"drudgery-articles\"]/div/a
            // #main > div.drudgery-column-3 > div:nth-child(2) > div:nth-child(1) > a
            nodes = doc.DocumentNode.SelectNodes("//div[@class=\"drudgery-column-3\"]/div[@class=\"drudgery-articles\"]/div/a");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(filters, node, "right column", count++);
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
