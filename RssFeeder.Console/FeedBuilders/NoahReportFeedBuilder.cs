using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.FeedBuilders
{
    internal class NoahReportFeedBuilder : BaseFeedBuilder, IRssFeedBuilder
    {
        public NoahReportFeedBuilder(ILogger log, IWebUtils webUtilities, IUtils utilities) : base(log, webUtilities, utilities)
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
            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));

            // Above the fold headline(s)
            // //div[@class=\"drudgery-top-links\"]/div/a
            var nodes = doc.DocumentNode.SelectNodes("//div.widget-area-top-1 > div.sl-links-main > ul > li > a");
            int count;
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

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
            nodes = doc.DocumentNode.SelectNodes("//div.widget-area-top-2 > div.sl-links-main > ul > li > a ");
            if (nodes != null)
            {
                count = 1;
                foreach (HtmlNode node in nodes)
                {
                    string title = WebUtility.HtmlDecode(node.InnerText.Trim());

                    var item = CreateNodeLinks(filters, node, "main headlines", count++, feedUrl);
                    if (item != null)
                    {
                        log.Debug("FOUND: {urlHash}|{linkLocation}|{title}|{url}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title, item.FeedAttributes.Url);
                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}
