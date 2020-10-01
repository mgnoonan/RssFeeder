using System.Collections.Generic;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class CitizenFreePressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public CitizenFreePressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
            int count;

            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // Above the Fold section
            var container = document.QuerySelector("ul.wpd-top-links");
            var nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
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
            container = document.QuerySelector("#featured");
            if (container != null)
            {
                nodes = container.QuerySelectorAll("a");
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
            container = document.QuerySelector("#column-1");
            nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
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
            container = document.QuerySelector("#column-2");
            nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
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

            // Column 3
            container = document.QuerySelector("#column-3");
            nodes = container.QuerySelectorAll("a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, "column 3", count++);
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
