using AngleSharp.Html.Parser;
using Antlr4.StringTemplate;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RssFeeder.Console.Models;
using RssFeeder.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.Exporters
{
    internal class BaseArticleExporter
    {
        public CrawlerConfig Config { get; set; }

        protected virtual string ApplyTemplateToDescription(RssFeedItem item, RssFeed feed, string template)
        {
            switch (item.SiteName)
            {
                case "youtube":
                    template = template.Replace("{class}", "");
                    template = template.Replace("{allow}", "accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture");
                    break;
                case "rumble":
                    template = template.Replace("{class}", "rumble");
                    template = template.Replace("{allow}", "");
                    break;
            }

            var t = new Template(template, '$', '$');
            t.Add("item", item);
            t.Add("feed", feed);
            t.Add("ArticleText", item.HtmlAttributes?.ContainsKey("ArticleText") ?? false ? item.HtmlAttributes["ArticleText"] : string.Empty);

            return t.Render();
        }

        protected virtual void SetExtendedArticleMetaData(ExportFeedItem item, HtmlDocument doc, string hostName)
        {
            // Extract the meta data from the Open Graph tags helpfully provided with almost every article
            string url = item.Url;
            item.Url = ParseOpenGraphMetaTagAttributes(doc, "og:url");

            if (string.IsNullOrWhiteSpace(item.Url) || hostName.Contains("frontpagemag.com"))
            {
                item.Url = url;
            }

            item.Subtitle = ParseOpenGraphMetaTagAttributes(doc, "og:title");
            item.ImageUrl = ParseOpenGraphMetaTagAttributes(doc, "og:image");
            item.HostName = hostName;
            item.SiteName = ParseOpenGraphMetaTagAttributes(doc, "og:site_name").ToLower();

            // Fixup apnews on populist press links which sometimes report incorrectly
            if (string.IsNullOrWhiteSpace(item.SiteName) || (item.SiteName == "ap news" && item.Url.Contains("populist.press")))
            {
                item.SiteName = item.HostName;
            }

            // Fixup news.trust.org imageUrl links which have an embedded redirect
            if (string.IsNullOrWhiteSpace(item.ImageUrl) || (item.SiteName == "news.trust.org" && item.Url.Contains("news.trust.org")))
            {
                item.ImageUrl = String.Empty;
            }

            // Remove the protocol portion if there is one, i.e. 'https://'
            if (item.SiteName.IndexOf('/') > 0)
            {
                item.SiteName = item.SiteName.Substring(item.SiteName.LastIndexOf('/') + 1);
            }
        }

        protected virtual void SetBasicArticleMetaData(ExportFeedItem item, string hostName)
        {
            item.HostName = hostName;
            item.SiteName = item.HostName;
            item.ArticleText = $"<p>Unable to crawl article content. Click the link below to view in your browser.</p>";
        }

        //protected virtual void SetVideoMetaData(ExportFeedItem exportFeedItem, RssFeedItem item)
        //{
        //    // Extract the meta data from the Open Graph tags customized for YouTube
        //    exportFeedItem.Subtitle = item.OpenGraphAttributes.ContainsKey("og:title") ? item.OpenGraphAttributes["og:title"] : "";
        //    exportFeedItem.ImageUrl = item.OpenGraphAttributes.ContainsKey("og:image") ? item.OpenGraphAttributes["og:image"] : "";
        //    exportFeedItem.SiteName = item.OpenGraphAttributes.ContainsKey("og:site_name") ? item.OpenGraphAttributes["og:site_name"] : "";

        //    if (string.IsNullOrWhiteSpace(item.SiteName))
        //    {
        //        exportFeedItem.SiteName = item.HostName;
        //    }

        //    if (item.HostName.Contains("rumble.com"))
        //    {
        //        var value = GetJsonDynamic<IEnumerable<dynamic>>(doc.Text, "script", "embedUrl");
        //        exportFeedItem.VideoUrl = value.First().embedUrl.Value;
        //        exportFeedItem.VideoHeight = int.TryParse(Convert.ToString(value.First().height.Value), out int height) ? height : 0;
        //        exportFeedItem.VideoWidth = int.TryParse(Convert.ToString(value.First().width.Value), out int width) ? width : 0;
        //    }
        //    else
        //    {
        //        // These may be YouTube-only Open Graph tags
        //        item.VideoUrl = ParseOpenGraphMetaTagAttributes(doc, "og:video:url");
        //        item.VideoHeight = int.TryParse(ParseOpenGraphMetaTagAttributes(doc, "og:video:height"), out int height) ? height : 0;
        //        item.VideoWidth = int.TryParse(ParseOpenGraphMetaTagAttributes(doc, "og:video:width"), out int width) ? width : 0;
        //    }
        //    Log.Information("Video URL: '{url}' ({height}x{width})", item.VideoUrl, item.VideoHeight, item.VideoWidth);

        //    // There's no article text for most video sites, so just use the meta description
        //    var description = ParseOpenGraphMetaTagAttributes(doc, "og:description");
        //    item.ArticleText = $"<p>{description}</p>";
        //}

        protected virtual T GetJsonDynamic<T>(string html, string tagName, string keyName)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // Query the document by CSS selectors to get the article text
            var blocks = document.QuerySelectorAll(tagName);
            if (blocks.Length == 0)
            {
            }

            string jsonRaw = string.Empty;
            foreach (var block in blocks)
            {
                if (block.TextContent.Contains(keyName))
                {
                    jsonRaw = block.TextContent;
                    break;
                }
            }

            return JsonConvert.DeserializeObject<T>(jsonRaw);
        }

        protected virtual void SetGraphicMetaData(RssFeedItem item, ExportFeedItem exportFeedItem)
        {
            // Extract the meta data from the Open Graph tags customized for YouTube
            exportFeedItem.ImageUrl = item.FeedAttributes.Url;
            exportFeedItem.HostName = item.HostName;
            exportFeedItem.SiteName = item.HostName;
        }

        protected virtual string ParseOpenGraphMetaTagAttributes(HtmlDocument doc, string targetAttributeValue)
        {
            string value = ParseMetaTagAttributes(doc, "property", targetAttributeValue, "content");

            if (string.IsNullOrEmpty(value))
            {
                value = ParseMetaTagAttributes(doc, "name", targetAttributeValue, "content");
            }

            return value;
        }

        protected virtual string ParseMetaTagAttributes(HtmlDocument doc, string targetAttributeName, string targetAttributeValue, string sourceAttributeName)
        {
            // Retrieve the requested meta tag by property name
            var node = doc.DocumentNode.SelectSingleNode($"//meta[@{targetAttributeName}='{targetAttributeValue}']");

            // Node can come back null if the meta tag is not present in the DOM
            // Attribute can come back null as well if not present on the meta tag
            string sourceAttributeValue = node?.Attributes[sourceAttributeName]?.Value.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sourceAttributeValue))
            {
                Log.Warning("Error reading attribute '{attribute}' from meta tag '{property}'", targetAttributeName, targetAttributeValue);
            }
            else
            {
                // Decode the value if it contains a coded reference
                if (sourceAttributeValue.Contains("&#x"))
                {
                    sourceAttributeValue = System.Web.HttpUtility.HtmlDecode(sourceAttributeValue);
                }

                Log.Information("Meta attribute '{attribute}':'{property}' has a decoded value of '{value}'", targetAttributeName, targetAttributeValue, sourceAttributeValue);

            }

            return sourceAttributeValue;
        }
    }
}
