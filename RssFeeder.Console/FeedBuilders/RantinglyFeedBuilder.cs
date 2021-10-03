using System.Collections.Generic;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
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
            int count;

            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // Above the Fold section
            var container = document.QuerySelector("ul.wpd-top-links");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, "above the fold", count++);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            // Main Headlines section
            container = document.QuerySelector("#content-wrap > div.page-header > div.the-content");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                if (nodes != null)
                {
                    count = 1;
                    foreach (var node in nodes)
                    {
                        string title = WebUtility.HtmlDecode(node.Text().Trim());

                        var item = CreateNodeLinks(filters, node, "main headlines", count++);
                        if (item != null && !item.Url.Contains("#the-comments") && !item.Url.Contains("#comment-"))
                        {
                            log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                            list.Add(item);
                        }
                    }
                }
            }

            // Column 1
            container = document.QuerySelector("#column-1 > div > div.wpd-posted-links");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, "column 1", count++);
                    if (item != null && !item.Url.Contains("#the-comments") && !item.Url.Contains("#comment-"))
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            // Column 2
            container = document.QuerySelector("#column-2 > div > div.wpd-posted-links");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, "column 2", count++);
                    if (item != null && !item.Url.Contains("#the-comments") && !item.Url.Contains("#comment-"))
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
