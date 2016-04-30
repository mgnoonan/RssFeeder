using HtmlAgilityPack;
using log4net;
using RssFeeder.Console.Models;
using RssFeeder.Console.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RssFeeder.Console.CustomBuilders
{
    class DrudgeReportFeedBuilder : ICustomFeedBuilder
    {
        public List<FeedItem> Build(ILog log, Feed feed)
        {
            var list = new List<FeedItem>();
            var filters = feed.Filters ?? new List<string>();

            string url = feed.Url;
            string channelTitle = feed.Title;
            string relativeRoot = feed.Url;

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
                    HtmlAttribute attr = headlineNode.Attributes["href"];
                    string linkUrl = attr.Value.Trim();

                    var imageNode = link.Descendants("img").FirstOrDefault();
                    string imageUrl = null;
                    if (imageNode != null)
                    {
                        attr = imageNode.Attributes["src"];
                        imageUrl = attr.Value.Trim();
                    }

                    string hash = Utility.Utility.CreateMD5Hash(linkUrl);
                    if (filters.Contains(hash))
                        continue;

                    if (!linkUrl.StartsWith("http"))
                        linkUrl = WebTools.MakeFullURL(relativeRoot, linkUrl);
                    if (imageUrl != null && !imageUrl.StartsWith("http"))
                        imageUrl = WebTools.MakeFullURL(relativeRoot, imageUrl);

                    if (linkUrl.Length > 0 && title.Length > 0)
                    {
                        log.InfoFormat("FOUND: {0}|{1}|{2}", hash, title, linkUrl);
                        var item = new FeedItem()
                        {
                            FeedId = feed.Id,
                            Title = HttpUtility.HtmlDecode(title),
                            //Description = Utility.Utility.GetDescriptionFromMeta(linkUrl),
                            Url = linkUrl,
                            UrlHash = hash,
                            DateAdded = DateTime.Now,
                            ImageUrl = imageUrl
                        };

                        list.Add(item);
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
                    HtmlAttribute attr = link.Attributes["href"];
                    string linkUrl = attr.Value.Trim();

                    string hash = Utility.Utility.CreateMD5Hash(linkUrl);
                    if (filters.Contains(hash))
                        continue;

                    if (!linkUrl.StartsWith("http"))
                        linkUrl = WebTools.MakeFullURL(relativeRoot, linkUrl);

                    if (linkUrl.Length > 0 && title.Length > 0)
                    {
                        log.InfoFormat("FOUND: {0}|{1}|{2}", hash, title, linkUrl);
                        var item = new FeedItem()
                        {
                            FeedId = feed.Id,
                            Title = HttpUtility.HtmlDecode(title),
                            //Description = Utility.Utility.GetDescriptionFromMeta(linkUrl),
                            Url = linkUrl,
                            UrlHash = hash,
                            DateAdded = DateTime.Now
                        };

                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}
