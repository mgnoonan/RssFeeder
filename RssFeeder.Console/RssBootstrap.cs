using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.StringTemplate;
using Autofac;
using HtmlAgilityPack;
using RssFeeder.Console.Database;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;
using StackExchange.Profiling;

namespace RssFeeder.Console
{
    public class RssBootstrap : IRssBootstrap
    {
        readonly IRepository repository;
        readonly IArticleParser parser;
        readonly IWebUtils webUtils;
        readonly IUtils utils;

        private const string MetaDataTemplate = @"
<p>
    The post <a href=""$item.Url$"">$item.Title$</a> captured from <a href=""$feed.Url$"">$feed.Title$</a> $item.LinkLocation$ on $item.DateAdded$ UTC.
</p>
<hr />
<p>
    <small>
    <ul>
        <li><strong>site_name:</strong> $item.SiteName$</li>
        <li><strong>host:</strong> $item.HostName$</li>
        <li><strong>url:</strong> <a href=""$item.Url$"">$item.Url$</a></li>
        <li><strong>captured:</strong> $item.DateAdded$ UTC</li>
        <li><strong>hash:</strong> $item.UrlHash$</li>
        <li><strong>location:</strong> $item.LinkLocation$</li>
    </ul>
    </small>
</p>
";

        private const string ExtendedTemplate = @"<img src=""$item.ImageUrl$"" />
<h3>$item.Subtitle$</h3>
$item.ArticleText$
" + MetaDataTemplate;

        private const string BasicTemplate = @"<h3>$item.Title$</h3>
" + MetaDataTemplate;

        /// <summary>
        /// The list of site definitions that describe how to get an article
        /// </summary>
        private static List<SiteArticleDefinition> ArticleDefinitions;

        public RssBootstrap(IRepository _repository, IArticleParser _parser, IWebUtils _webUtils, IUtils _utils)
        {
            repository = _repository;
            parser = _parser;
            webUtils = _webUtils;
            utils = _utils;
        }

        public void Start(IContainer container, MiniProfiler profiler, RssFeed feed)
        {
            string html = webUtils.DownloadString(feed.Url);

            // Create the working folder for the collection if it doesn't exist
            string workingFolder = Path.Combine(utils.GetAssemblyDirectory(), feed.CollectionName);
            if (!Directory.Exists(workingFolder))
            {
                Log.Logger.Information("Creating folder '{workingFolder}'", workingFolder);
                Directory.CreateDirectory(workingFolder);
            }

            // Save the feed html source for posterity
            string fileStem = Path.Combine(workingFolder, $"{DateTime.Now.ToUniversalTime():yyyyMMddhhmmss}_{feed.Url.Replace("://", "_").Replace(".", "_").Replace("/", "")}");
            utils.SaveTextToDisk(html, fileStem + ".html", false);

            // Save thumbnail snapshot of the page
            webUtils.SaveThumbnailToDisk(feed.Url, fileStem + ".png");

            // Parse the target links from the source to build the article crawl list
            var builder = container.ResolveNamed<IRssFeedBuilder>(feed.CollectionName);
            var list = builder.ParseRssFeedItems(feed, html)
                //.Take(10) FOR DEBUG PURPOSES
                ;

            // Load the collection of site parsers
            if (ArticleDefinitions == null || !ArticleDefinitions.Any())
            {
                ArticleDefinitions = repository.GetDocuments<SiteArticleDefinition>("site-parsers", q => q.ArticleSelector.Length > 0);
            }

            // Crawl any new articles and add them to the database
            Log.Logger.Information("Adding new articles to the {collectionName} collection", feed.CollectionName);
            using (profiler.Step("Adding new articles"))
            {
                int count = 0;
                foreach (var item in list)
                {
                    // With expression wrapped around func
                    //Expression<Func<RssFeedItem, bool>> predicate = q => q.UrlHash == item.UrlHash;
                    //bool exists = profiler.Inline<bool>(() => repository.DocumentExists<RssFeedItem>(feed.CollectionName, predicate), "DocumentExists");

                    bool exists = profiler.Inline<bool>(() => repository.DocumentExists(feed.CollectionName, item.UrlHash), "DocumentExists");

                    if (!exists)
                    {
                        count++;

                        // Construct unique file name
                        string friendlyHostname = item.Url.Replace("://", "_").Replace(".", "_");
                        friendlyHostname = friendlyHostname.Substring(0, friendlyHostname.IndexOf("/"));
                        string filename = Path.Combine(workingFolder, $"{item.UrlHash}_{friendlyHostname}.html");
                        item.FileName = webUtils.SaveUrlToDisk(item.Url, item.UrlHash, filename);

                        using (StreamReader reader = File.OpenText(filename))
                        {
                            var value = webUtils.StripJavascriptAndCss(reader.ReadToEnd());
                            filename = Path.Combine(workingFolder, $"{item.UrlHash}_{friendlyHostname}.clean.html");
                            utils.SaveTextToDisk(value, filename, true);
                        }

                        ParseArticleMetaTags(item, feed, ArticleDefinitions?.SingleOrDefault(p => p.SiteName == item.SiteName));
                        repository.CreateDocument<RssFeedItem>(feed.CollectionName, item);
                    }
                }
                Log.Logger.Information("Added {count} new articles to the {collectionName} collection", count, feed.CollectionName);
            }

            // Purge stale files from working folder
            short maximumAgeInDays = 7;
            utils.PurgeStaleFiles(workingFolder, maximumAgeInDays);

            // Purge stale documents from the database collection
            list = repository.GetDocuments<RssFeedItem>(feed.CollectionName, q => q.DateAdded <= DateTime.Now.AddDays(-maximumAgeInDays));
            foreach (var item in list)
            {
                Log.Logger.Information("Removing UrlHash '{urlHash}' from {collectionName}", item.UrlHash, feed.CollectionName);
                repository.DeleteDocument<RssFeedItem>(feed.CollectionName, item.Id, item.HostName);
            }

            Log.Logger.Information("Removed {count} documents older than {maximumAgeInDays} days from {collectionName}", list.Count(), 7, feed.CollectionName);
        }

        private void ParseArticleMetaTags(RssFeedItem item, RssFeed feed, SiteArticleDefinition definition)
        {
            if (File.Exists(item.FileName))
            {
                // Article was successfully downloaded from the target site
                Log.Logger.Information("Parsing meta tags from file '{fileName}'", item.FileName);

                var doc = new HtmlDocument();
                doc.Load(item.FileName);

                if (!doc.DocumentNode.HasChildNodes)
                {
                    Log.Logger.Warning("No file content found, skipping.");
                    SetBasicArticleMetaData(item);
                    return;
                }

                // Meta tags provide extended data about the item, display as much as possible
                SetExtendedArticleMetaData(item, doc, definition);
                item.Description = ApplyTemplateToDescription(item, feed, ExtendedTemplate);
            }
            else
            {
                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(item);
                item.Description = ApplyTemplateToDescription(item, feed, BasicTemplate);
            }

            Log.Logger.Debug("{@item}", item);
        }

        private string ApplyTemplateToDescription(RssFeedItem item, RssFeed feed, string template)
        {
            var t = new Template(template, '$', '$');
            t.Add("item", item);
            t.Add("feed", feed);

            return t.Render();
        }

        private void SetExtendedArticleMetaData(RssFeedItem item, HtmlDocument doc, SiteArticleDefinition definition)
        {
            // Extract the meta data from the Open Graph tags helpfully provided with almost every article
            item.Subtitle = ParseMetaTagAttributes(doc, "og:title", "content");
            item.ImageUrl = ParseMetaTagAttributes(doc, "og:image", "content");
            item.MetaDescription = ParseMetaTagAttributes(doc, "og:description", "content");
            item.HostName = new Uri(item.Url).GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
            item.SiteName = ParseMetaTagAttributes(doc, "og:site_name", "content").ToLower();
            if (string.IsNullOrWhiteSpace(item.SiteName))
            {
                item.SiteName = item.HostName;
            }

            // Check if we have a site parser defined for the site name
            //var definition = ArticleDefinitions?.SingleOrDefault(p => p.SiteName == item.SiteName);

            if (definition == null)
            {
                // We don't have an article parser definition for this site, so just use the meta description
                item.ArticleText = $"<p>{item.MetaDescription}</p>";
            }
            else
            {
                // Add a cached instance of this parser if we don't already have one, using reflection
                //if (!ArticleParserCache.ContainsKey(item.SiteName))
                //{
                //    Type type = Assembly.GetExecutingAssembly().GetType(definition.Parser);
                //    ArticleParserCache.Add(item.SiteName, (IArticleParser)Activator.CreateInstance(type));
                //}

                // Parse the article from the html
                //var inst = ArticleParserCache[item.SiteName];
                item.ArticleText = parser.GetArticleBySelector(doc.Text, definition);
            }
        }

        private void SetBasicArticleMetaData(RssFeedItem item)
        {
            item.HostName = new Uri(item.Url).GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
            item.SiteName = item.HostName;
            item.ArticleText = $"<p>Unable to crawl article content. Click the link below to view in your browser.</p>";
        }

        private string ParseMetaTagAttributes(HtmlDocument doc, string property, string attribute)
        {
            // Retrieve the requested meta tag by property name
            var node = doc.DocumentNode.SelectSingleNode($"/html/head/meta[@property='{property}']");

            // Node can come back null if the meta tag is not present in the DOM
            // Attribute can come back null as well if not present on the meta tag
            string value = node?.Attributes[attribute]?.Value.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                Log.Logger.Warning("Error reading attribute '{attribute}' from meta tag '{property}'", attribute, property);
            }

            return value;
        }
    }
}
