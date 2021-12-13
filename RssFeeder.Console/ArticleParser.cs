﻿using System;
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
using RssFeeder.Console.TagParsers;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;
using Serilog.Context;

namespace RssFeeder.Console
{
    public class ArticleParser : IArticleParser
    {
        private IContainer _container;
        private IArticleDefinitionFactory _definitionFactory;
        private IWebUtils _webUtils;

        public CrawlerConfig Config { get; set; }

        public void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory, IWebUtils webUtils)
        {
            _container = container;
            _definitionFactory = definitionFactory;
            _webUtils = webUtils;
        }

        public void Parse(RssFeedItem item)
        {
            // Article failed to download for some reason, skip over meta data processing
            if (!File.Exists(item.FeedAttributes.FileName))
            {
                Log.Information("No file to parse, skipping metadata values for '{url}'", item.FeedAttributes.Url);
                return;
            }

            // Graphics file or PDF won't have og tags
            if (item.FeedAttributes.FileName.EndsWith(".png") ||
                item.FeedAttributes.FileName.EndsWith(".jpg") ||
                item.FeedAttributes.FileName.EndsWith(".gif") ||
                item.FeedAttributes.FileName.EndsWith(".pdf"))
            {
                Log.Information("Graphics file detected, skipping metadata values for '{url}'", item.FeedAttributes.Url);
                return;
            }

            Log.Information("Parsing meta tags from file '{fileName}'", item.FeedAttributes.FileName);

            var doc = new HtmlDocument();
            doc.Load(item.FeedAttributes.FileName);

            // Parse the meta data from the raw HTML document
            item.OpenGraphAttributes.Add(ParseOpenGraphAttributes(doc));
            item.HtmlAttributes.Add(ParseHtmlAttributes(doc));
            item.HostName = GetHostName(item);
            item.SiteName = GetSiteName(item);

            // Check if we have a site parser defined for the site name
            var definition = _definitionFactory.Get(item.SiteName);

            using (LogContext.PushProperty("siteName", item.SiteName))
            {
                // If a specific article parser was not found in the database then
                // use the fallback adaptive parser
                var parser = _container.ResolveNamed<ITagParser>(definition?.Parser ?? "adaptive-parser");
                item.HtmlAttributes.Add("ParserResult", parser.ParseTagsBySelector(doc.Text, definition));
            }
        }

        private string GetHostName(RssFeedItem item)
        {
            string url = item.OpenGraphAttributes.ContainsKey("og:url") ?
                            item.OpenGraphAttributes["og:url"].ToLower() :
                            item.FeedAttributes.Url.ToLower();

            if (!url.StartsWith("http"))
            {
                url = _webUtils.RepairUrl(url, item.FeedAttributes.Url.ToLower());
            }

            Uri uri = new Uri(url);
            return uri.GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
        }

        private string GetSiteName(RssFeedItem item)
        {
            string siteName = item.OpenGraphAttributes.ContainsKey("og:site_name") ?
                                item.OpenGraphAttributes["og:site_name"].ToLower() :
                                item.HostName;

            return siteName;
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
