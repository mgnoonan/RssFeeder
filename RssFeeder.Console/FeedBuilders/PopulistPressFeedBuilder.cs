using System.Collections.Generic;
using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class PopulistPressFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public PopulistPressFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
                if (item.Url.StartsWith("/"))
                {
                    item.Url = feedUrl + item.Url;
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
            var container = document.QuerySelector("#category-posts-9-internal");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(filters, node, "above the fold", count++);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            // Main Headlines section
            container = document.QuerySelector("#category-posts-10-internal");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                if (nodes != null)
                {
                    count = 1;
                    foreach (var node in nodes)
                    {
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
            container = document.QuerySelector("#home_page_left_column");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(filters, node, "column 1", count++);

                    // Unfortunately the reference site links are included in the column links, so the
                    // AMERICAN THINKER link signals the end of the article list in column 1
                    if (item.Url.Contains("americanthinker.com"))
                        break;

                    if (item != null && !item.Url.Contains("#the-comments") && !item.Url.Contains("#comment-"))
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            // Column 2
            container = document.QuerySelector("#home_page_middle_column");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());
                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    Log.Information("Checking column 2 title '{title}'", title);

                    var item = CreateNodeLinks(filters, node, "column 2", count++);

                    // Unfortunately the reference site links are included in the column links, so the
                    // CINDY ADAMS link signals the end of the article list in column 2
                    if (item.Url.Contains("cindy-adams"))
                        break;

                    if (item != null && !item.Url.Contains("#the-comments") && !item.Url.Contains("#comment-"))
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.UrlHash, item.LinkLocation, item.Title, item.Url);
                        list.Add(item);
                    }
                }
            }

            // Column 3
            container = document.QuerySelector("#home_page_right_column");
            if (container != null)
            {
                var nodes = container.QuerySelectorAll("a");
                count = 1;
                foreach (var node in nodes)
                {
                    var item = CreateNodeLinks(filters, node, "column 3", count++);

                    // Unfortunately the reference site links are included in the column links, so the
                    // PRIVACY POLICY link signals the end of the article list in column 2
                    if (item.Url.Contains("privacy-policy-2"))
                        break;

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
