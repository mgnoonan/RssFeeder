using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Html.Parser;
using Antlr4.StringTemplate;
using Autofac;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RssFeeder.Console.ArticleDefinitions;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Models;
using Serilog;
using Serilog.Context;

namespace RssFeeder.Console
{
    public class ArticleParser : IArticleParser
    {
        private IContainer _container;
        private IArticleDefinitionFactory _definitionFactory;

        public CrawlerConfig Config { get; set; }

        public void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory)
        {
            _container = container;
            _definitionFactory = definitionFactory;
        }

        public void Parse(RssFeedItem item, RssFeed feed)
        {
            Uri uri = new Uri(item.Url);
            string hostName = uri.GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();

            if (File.Exists(item.FeedAttributes.FileName))
            {
                Log.Information("Parsing meta tags from file '{fileName}'", item.FeedAttributes.FileName);

                if (item.FeedAttributes.FileName.EndsWith(".png") ||
                    item.FeedAttributes.FileName.EndsWith(".jpg") ||
                    item.FeedAttributes.FileName.EndsWith(".gif"))
                {
                    SetGraphicMetaData(item, hostName);
                    item.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.GraphicTemplate);
                }
                else
                {
                    var doc = new HtmlDocument();
                    doc.Load(item.FeedAttributes.FileName);

                    item.OpenGraphAttributes = ParseOpenGraphAttributes(doc);
                    item.HtmlAttributes = ParseHtmlAttributes(doc);

                    // Meta tags provide extended data about the item, display as much as possible
                    if (Config.VideoHosts.Contains(hostName))
                    {
                        SetVideoMetaData(item, doc, hostName);
                        if (item.VideoHeight > 0)
                        {
                            item.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.VideoTemplate);
                        }
                        else
                        {
                            item.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.ExtendedTemplate);
                        }
                    }
                    else
                    {
                        SetExtendedArticleMetaData(item, doc, hostName);
                        item.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.ExtendedTemplate);
                    }
                }
            }
            else
            {
                Log.Information("No file to parse, applying basic metadata values for '{hostname}'", hostName);

                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(item, hostName);
                item.Description = ApplyTemplateToDescription(item, feed, ExportTemplates.BasicTemplate);
            }

            Log.Debug("{@item}", item);
        }

        private string ApplyTemplateToDescription(RssFeedItem item, RssFeed feed, string template)
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

        private void SetExtendedArticleMetaData(RssFeedItem item, HtmlDocument doc, string hostName)
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

            // Check if we have a site parser defined for the site name
            var definition = _definitionFactory.Get(item.SiteName);

            using (LogContext.PushProperty("siteName", item.SiteName))
            {
                if (definition == null)
                {
                    Log.Information("No parsing definition found for '{siteName}' on hash '{urlHash}'", item.SiteName, item.UrlHash);

                    // If a specific article parser was not found in the database then
                    // use the fallback adaptive parser (experimental)
                    var parser = _container.ResolveNamed<ITagParser>("adaptive-parser");
                    item.HtmlAttributes.Add("ArticleText", parser.GetArticleBySelector(doc.Text, definition));
                }
                else
                {
                    // Resolve the parser defined for the site
                    var parser = _container.ResolveNamed<ITagParser>(definition.Parser);
                    // Parse the article
                    item.HtmlAttributes.Add("ArticleText", parser.GetArticleBySelector(doc.Text, definition));
                }
            }
        }

        private void SetBasicArticleMetaData(RssFeedItem item, string hostName)
        {
            item.HostName = hostName;
            item.SiteName = item.HostName;
            item.ArticleText = $"<p>Unable to crawl article content. Click the link below to view in your browser.</p>";
        }

        private void SetVideoMetaData(RssFeedItem item, HtmlDocument doc, string hostName)
        {
            // Extract the meta data from the Open Graph tags customized for YouTube
            item.Subtitle = ParseOpenGraphMetaTagAttributes(doc, "og:title");
            item.ImageUrl = ParseOpenGraphMetaTagAttributes(doc, "og:image");
            item.HostName = hostName;
            item.SiteName = ParseOpenGraphMetaTagAttributes(doc, "og:site_name").ToLower();

            if (string.IsNullOrWhiteSpace(item.SiteName))
            {
                item.SiteName = item.HostName;
            }

            if (item.HostName.Contains("rumble.com"))
            {
                var value = GetJsonDynamic<IEnumerable<dynamic>>(doc.Text, "script", "embedUrl");
                item.VideoUrl = value.First().embedUrl.Value;
                item.VideoHeight = int.TryParse(Convert.ToString(value.First().height.Value), out int height) ? height : 0;
                item.VideoWidth = int.TryParse(Convert.ToString(value.First().width.Value), out int width) ? width : 0;
            }
            else
            {
                // These may be YouTube-only Open Graph tags
                item.VideoUrl = ParseOpenGraphMetaTagAttributes(doc, "og:video:url");
                item.VideoHeight = int.TryParse(ParseOpenGraphMetaTagAttributes(doc, "og:video:height"), out int height) ? height : 0;
                item.VideoWidth = int.TryParse(ParseOpenGraphMetaTagAttributes(doc, "og:video:width"), out int width) ? width : 0;
            }
            Log.Information("Video URL: '{url}' ({height}x{width})", item.VideoUrl, item.VideoHeight, item.VideoWidth);

            // There's no article text for most video sites, so just use the meta description
            var description = ParseOpenGraphMetaTagAttributes(doc, "og:description");
            item.ArticleText = $"<p>{description}</p>";
        }

        private T GetJsonDynamic<T>(string html, string tagName, string keyName)
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

        private void SetGraphicMetaData(RssFeedItem item, string hostName)
        {
            // Extract the meta data from the Open Graph tags customized for YouTube
            item.ImageUrl = item.Url;
            item.HostName = hostName;
            item.SiteName = item.HostName;
        }

        private Dictionary<string, string> ParseOpenGraphAttributes(HtmlDocument doc)
        {
            var attributes = new Dictionary<string, string>();
            var nodes = doc.DocumentNode.SelectNodes($"//meta");
            if (nodes is null)
            {
                return attributes;
            }

            foreach (var node in nodes)
            {
                string propertyValue = node.Attributes["property"]?.Value ??
                    node.Attributes["name"]?.Value ?? "";

                if (propertyValue.StartsWith("og:"))
                {
                    string contentValue = node.Attributes["content"]?.Value ?? "unspecified";
                    Log.Information("Found open graph attribute '{propertyValue}':'{contentValue}'", propertyValue, contentValue);

                    if (!attributes.ContainsKey(propertyValue))
                    {
                        attributes.Add(propertyValue, contentValue);
                    }
                    else
                    {
                        Log.Warning("Duplicate open graph tag '{propertyValue}'", propertyValue);
                    }
                }
            }

            return attributes;
        }

        private Dictionary<string, string> ParseHtmlAttributes(HtmlDocument doc)
        {
            var attributes = new Dictionary<string, string>();

            // Title tag
            var node = doc.DocumentNode.SelectSingleNode($"//title");
            string contentValue = node?.InnerText.Trim() ?? string.Empty;
            attributes.Add("title", contentValue);

            // H1 values for possible headline - may not exist
            node = doc.DocumentNode.SelectSingleNode($"//h1");
            contentValue = node?.InnerText.Trim() ?? string.Empty;
            attributes.Add("h1", contentValue);

            // Description meta tag - may not exist
            attributes.Add("description", ParseMetaTagAttributes(doc, "name", "description", "content"));

            return attributes;
        }

        private string ParseOpenGraphMetaTagAttributes(HtmlDocument doc, string targetAttributeValue)
        {
            string value = ParseMetaTagAttributes(doc, "property", targetAttributeValue, "content");

            if (string.IsNullOrEmpty(value))
            {
                value = ParseMetaTagAttributes(doc, "name", targetAttributeValue, "content");
            }

            return value;
        }

        private string ParseMetaTagAttributes(HtmlDocument doc, string targetAttributeName, string targetAttributeValue, string sourceAttributeName)
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
