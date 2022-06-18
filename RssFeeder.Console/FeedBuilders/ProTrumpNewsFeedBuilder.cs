using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;

namespace RssFeeder.Console.FeedBuilders;

internal class ProTrumpNewsFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
{
    public ProTrumpNewsFeedBuilder(ILogger logger, IWebUtils webUtilities, IUtils utilities) : base(logger, webUtilities, utilities)
    {
    }

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

        // Above the Fold section
        var container = document.QuerySelector("div.widget-area-top-1 > div.sl-links-main");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "above the fold", count++, feedUrl);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Featured headline(s)
        // #link-70828 > a
        container = document.QuerySelector("div.widget-area-top-2 > div.sl-links-main");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        // Column 1
        // #link-70828 > a
        container = document.QuerySelector("div.homepage-column-1 > div.sl-links-main");
        if (container != null)
        {
            var nodes = container.QuerySelectorAll("a");
            count = 1;
            foreach (var node in nodes)
            {
                var item = CreateNodeLinks(filters, node, "column 1", count++, feedUrl);
                if (item != null)
                {
                    log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                    list.Add(item);
                }
            }
        }

        //// Column 2
        //// #link-70828 > a
        //container = document.QuerySelector("div.homepage-column-2 > div.sl-links-main");
        //if (container != null)
        //{
        //    var nodes = container.QuerySelectorAll("a");
        //    count = 1;
        //    foreach (var node in nodes)
        //    {
        //        var item = CreateNodeLinks(filters, node, "column 2", count++, feedUrl);
        //        if (item != null)
        //        {
        //            log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
        //            list.Add(item);
        //        }
        //    }
        //}

        //// Column 3
        //// #link-70828 > a
        //container = document.QuerySelector("div.homepage-column-3 > div.sl-links-main");
        //if (container != null)
        //{
        //    var nodes = container.QuerySelectorAll("a");
        //    count = 1;
        //    foreach (var node in nodes)
        //    {
        //        var item = CreateNodeLinks(filters, node, "column 3", count++, feedUrl);
        //        if (item != null)
        //        {
        //            log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
        //            list.Add(item);
        //        }
        //    }
        //}

        return list;
    }
}
