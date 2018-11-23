using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using HtmlAgilityPack;
using log4net;
using RssFeeder.Console.Utility;
using RssFeeder.Models;

namespace RssFeeder.Console.CustomBuilders
{
    class DrudgeReportFeedBuilder : IRssFeedBuilder
    {
        public List<RssFeedItem> ParseRssFeedItems(ILog log, RssFeed feed)
        {
            var list = new List<RssFeedItem>();
            var filters = feed.Filters ?? new List<string>();

            string url = feed.Url;
            string channelTitle = feed.Title;

            string html = WebTools.GetUrl(url);
            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));


            //
            // Headline link and title
            //

            var nodes = doc.DocumentNode.SelectNodes("//center");
            foreach (HtmlNode link in nodes)
            {
                if (!link.InnerHtml.Contains("MAIN HEADLINE"))
                    continue;

                var headlineNode = link.Descendants("a").FirstOrDefault();
                if (headlineNode != null)
                {
                    string title = HttpUtility.HtmlDecode(headlineNode.InnerText.Trim());

                    // Replace all errant spaces, which sometimes creep into Drudge's URLs
                    HtmlAttribute attr = headlineNode.Attributes["href"];
                    string linkUrl = attr.Value.Trim().Replace(" ", string.Empty).ToLower();

                    // Repair any protocol typos if possible
                    if (!linkUrl.StartsWith("http"))
                    {
                        log.Info($"Attempting to repair link '{linkUrl}'");
                        linkUrl = WebTools.RepairUrl(feed.Url, linkUrl);
                    }

                    // Calculate the MD5 hash for the link so we can be sure of uniqueness
                    string hash = Utility.Utility.CreateMD5Hash(linkUrl);
                    if (filters.Contains(hash))
                    {
                        log.Debug($"Hash '{hash}' found in filter list");
                        continue;
                    }

                    if (linkUrl.Length > 0 && title.Length > 0)
                    {
                        log.Info($"FOUND: {hash}|{title}|{linkUrl}");
                        list.Add(new RssFeedItem()
                        {
                            Id = Guid.NewGuid().ToString(),
                            FeedId = feed.Id,
                            Title = HttpUtility.HtmlDecode(title),
                            Url = linkUrl,
                            UrlHash = hash,
                            DateAdded = DateTime.Now.ToUniversalTime()
                        });
                    }
                }
            }


            //
            // All links that end in '...'
            //

            nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            foreach (HtmlNode link in nodes)
            {
                string title = HttpUtility.HtmlDecode(link.InnerText.Trim());

                if (title.EndsWith("...") || title.EndsWith("?") || title.EndsWith("!"))
                {
                    // Replace all errant spaces, which sometimes creep into Drudge's URLs
                    HtmlAttribute attr = link.Attributes["href"];
                    string linkUrl = attr.Value.Trim().Replace(" ", string.Empty).ToLower();

                    // Repair any protocol typos if possible
                    if (!linkUrl.StartsWith("http"))
                    {
                        log.Info($"Attempting to repair link '{linkUrl}'");
                        linkUrl = WebTools.RepairUrl(feed.Url, linkUrl);
                    }

                    // Calculate the MD5 hash for the link so we can be sure of uniqueness
                    string hash = Utility.Utility.CreateMD5Hash(linkUrl);
                    if (filters.Contains(hash))
                    {
                        log.Debug($"Hash '{hash}' found in filter list");
                        continue;
                    }

                    if (linkUrl.Length > 0 && title.Length > 0)
                    {
                        log.InfoFormat("FOUND: {0}|{1}|{2}", hash, title, linkUrl);
                        list.Add(new RssFeedItem()
                        {
                            Id = Guid.NewGuid().ToString(),
                            FeedId = feed.Id,
                            Title = HttpUtility.HtmlDecode(title),
                            Url = linkUrl,
                            UrlHash = hash,
                            DateAdded = DateTime.Now.ToUniversalTime()
                        });
                    }
                }
            }

            return list;
        }
    }
}
