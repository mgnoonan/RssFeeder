using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    internal class RevolverNewsFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public RevolverNewsFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) : 
            base(logger, webUtilities, utilities)
        { }

        public List<RssFeedItem> GenerateRssFeedItemList(RssFeed feed, string html)
        {
            return GenerateRssFeedItemList(feed.CollectionName, feed.Url, feed.Filters, html);
        }

        public List<RssFeedItem> GenerateRssFeedItemList(string feedCollectionName, string feedUrl, List<string> feedFilters, string html)
        {
            var items = GenerateRssFeedItemList(html, feedFilters ?? new List<string>(), feedUrl);
            PostProcessing(feedCollectionName, feedUrl, items);

            return items;
        }

        public List<RssFeedItem> GenerateRssFeedItemList(string html, List<string> filters, string feedUrl)
        {
            var list = new List<RssFeedItem>();
            int count;

            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // Featured links section
            var container = document.QuerySelector("div.revolver > div.column.center > div.post-listing");
            var nodes = container.QuerySelectorAll("div.title > a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, "feature links", count++, feedUrl);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }

            // Stories section
            container = document.QuerySelector("div.revolver > div.column.center > div.articles-wrapper > div.infinite-content");
            nodes = container.QuerySelectorAll("div.title > a");
            if (nodes != null)
            {
                count = 1;
                foreach (var node in nodes.Take(5))
                {
                    string title = WebUtility.HtmlDecode(node.Text().Trim());

                    var item = CreateNodeLinks(filters, node, "news feed", count++, feedUrl);
                    if (item != null)
                    {
                        log.Information("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}
