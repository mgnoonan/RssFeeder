using System;
using System.IO;
using System.Linq;
using Antlr4.StringTemplate;
using Autofac;
using HtmlAgilityPack;
using RssFeeder.Console.ArticleDefinitions;
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
        readonly IArticleDefinitionFactory definitions;
        readonly IWebUtils webUtils;
        readonly IUtils utils;

        private IArticleParser primaryParser;
        private IArticleParser fallbackParser;

        private const string _collectionName = "drudge-report";
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

        private const string YouTubeTemplate = @"<iframe width=""$item.VideoWidth$"" height=""$item.VideoHeight$"" src=""$item.VideoUrl$"" frameborder=""0"" allow=""accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture"" allowfullscreen></iframe>
<h3>$item.Subtitle$</h3>
$item.ArticleText$
" + MetaDataTemplate;

        private const string GraphicTemplate = @"<img src=""$item.ImageUrl$"" />
" + MetaDataTemplate;

        private const string BasicTemplate = @"<h3>$item.Title$</h3>
" + MetaDataTemplate;

        public RssBootstrap(IRepository _repository, IArticleDefinitionFactory _definitions, IWebUtils _webUtils, IUtils _utils)
        {
            repository = _repository;
            webUtils = _webUtils;
            utils = _utils;
            definitions = _definitions;
        }

        public void Start(IContainer container, MiniProfiler profiler, RssFeed feed)
        {
            string html = webUtils.DownloadStringWithCompression(feed.Url);

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
            primaryParser = container.ResolveNamed<IArticleParser>("generic-parser");
            fallbackParser = container.ResolveNamed<IArticleParser>("adaptive-parser");
            var list = builder.ParseRssFeedItems(feed, html);

            // Crawl any new articles and add them to the database
            Log.Logger.Information("Adding new articles to the {collectionName} collection", feed.CollectionName);
            using (profiler.Step("Adding new articles"))
            {
                int count = 0;
                foreach (var item in list)
                {
                    // No need to continue if we already crawled the article
                    if (repository.DocumentExists<RssFeedItem>(_collectionName, feed.CollectionName, item.UrlHash))
                        continue;

                    // Increment for new article crawl
                    count++;

                    Uri uri = new Uri(item.Url);
                    string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower();
                    string extension = path.EndsWith(".png") ? ".png" : path.EndsWith(".jpg") ? ".jpg" : ".html";

                    // Construct unique file name
                    string friendlyHostname = item.Url.Replace("://", "_").Replace(".", "_");
                    friendlyHostname = friendlyHostname.Substring(0, friendlyHostname.IndexOf("/"));
                    string filename = Path.Combine(workingFolder, $"{item.UrlHash}_{friendlyHostname}{extension}");

                    // Download the Url contents, first using HttpClient but if that fails use Selenium
                    item.FileName = webUtils.SaveUrlToDisk(item.Url, item.UrlHash, filename);
                    if (string.IsNullOrEmpty(item.FileName))
                    {
                        // Must have had an error on loading the url so attempt with Selenium
                        item.FileName = webUtils.WebDriverUrlToDisk(item.Url, item.UrlHash, filename);
                    }

                    // Parse the saved file as dictated by the site definitions
                    ParseArticleMetaTags(item, feed, definitions);
                    repository.CreateDocument<RssFeedItem>(_collectionName, item);
                }

                Log.Logger.Information("Added {count} new articles to the {collectionName} collection", count, feed.CollectionName);
            }

            // Purge stale files from working folder
            short maximumAgeInDays = 7;
            utils.PurgeStaleFiles(workingFolder, maximumAgeInDays);

            // Purge stale documents from the database collection
            list = repository.GetDocuments<RssFeedItem>(_collectionName, $"SELECT c.id, c.UrlHash, c.HostName FROM c WHERE c.DateAdded <= '{DateTime.UtcNow.AddDays(-maximumAgeInDays):o}' AND (c.FeedId = '{feed.CollectionName}' OR c.FeedId = 0)");
            //list = repository.GetDocuments<RssFeedItem>(_collectionName,
            //    $@"from index 'Auto/AllDocs/ByDateAddedAndFeedId'
            //       where DateAdded <= '{DateTime.UtcNow.AddDays(-maximumAgeInDays):o}' 
            //       and FeedId = '{feed.CollectionName}'");

            foreach (var item in list)
            {
                Log.Logger.Information("Removing UrlHash '{urlHash}' from {collectionName}", item.UrlHash, feed.CollectionName);
                repository.DeleteDocument<RssFeedItem>(_collectionName, item.Id, item.HostName);
            }

            Log.Logger.Information("Removed {count} documents older than {maximumAgeInDays} days from {collectionName}", list.Count(), 7, feed.CollectionName);
        }

        private void ParseArticleMetaTags(RssFeedItem item, RssFeed feed, IArticleDefinitionFactory definitions)
        {
            Log.Logger.Information("Parsing meta tags from file '{fileName}'", item.FileName);

            Uri uri = new Uri(item.Url);
            string hostName = uri.GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();

            if (File.Exists(item.FileName))
            {
                if (item.FileName.EndsWith(".png") || item.FileName.EndsWith(".jpg"))
                {
                    SetGraphicMetaData(item, hostName);
                    item.Description = ApplyTemplateToDescription(item, feed, GraphicTemplate);
                }
                else
                {
                    var doc = new HtmlDocument();
                    doc.Load(item.FileName);

                    // Meta tags provide extended data about the item, display as much as possible
                    if (hostName == "www.youtube.com")
                    {
                        SetYouTubeMetaData(item, doc, hostName);
                        item.Description = ApplyTemplateToDescription(item, feed, YouTubeTemplate);
                    }
                    else
                    {
                        SetExtendedArticleMetaData(item, doc, definitions, hostName);
                        item.Description = ApplyTemplateToDescription(item, feed, ExtendedTemplate);
                    }
                }
            }
            else
            {
                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(item, hostName);
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

        private void SetExtendedArticleMetaData(RssFeedItem item, HtmlDocument doc, IArticleDefinitionFactory definitions, string hostName)
        {
            // Extract the meta data from the Open Graph tags helpfully provided with almost every article
            item.Subtitle = ParseMetaTagAttributes(doc, "og:title", "content");
            item.ImageUrl = ParseMetaTagAttributes(doc, "og:image", "content");
            item.MetaDescription = ParseMetaTagAttributes(doc, "og:description", "content");
            item.HostName = hostName;
            item.SiteName = ParseMetaTagAttributes(doc, "og:site_name", "content").ToLower();

            if (string.IsNullOrWhiteSpace(item.SiteName))
            {
                item.SiteName = item.HostName;
            }

            // Remove the protocol portion if there is one, i.e. 'https://'
            if (item.SiteName.IndexOf('/') > 0)
            {
                item.SiteName = item.SiteName.Substring(item.SiteName.LastIndexOf('/') + 1);
            }

            // Check if we have a site parser defined for the site name
            var definition = definitions.Get(item.SiteName);

            if (definition == null)
            {
                Log.Warning("No parsing definition found for '{siteName}' on hash '{urlHash}'", item.SiteName, item.UrlHash);

                // If a specific article parser was not found in the database then
                // use the fallback adaptive parser (experimental)
                string articleText = fallbackParser.GetArticleBySelector(doc.Text, definition);
                if (string.IsNullOrEmpty(articleText))
                {
                    item.ArticleText = $"<p>{item.MetaDescription}</p>";
                }
                else
                {
                    item.ArticleText = articleText;
                }
            }
            else
            {
                // Parse the article from the default parser and definitions
                item.ArticleText = primaryParser.GetArticleBySelector(doc.Text, definition);
            }
        }

        private void SetBasicArticleMetaData(RssFeedItem item, string hostName)
        {
            item.HostName = hostName;
            item.SiteName = item.HostName;
            item.ArticleText = $"<p>Unable to crawl article content. Click the link below to view in your browser.</p>";
        }

        private void SetYouTubeMetaData(RssFeedItem item, HtmlDocument doc, string hostName)
        {
            // Extract the meta data from the Open Graph tags customized for YouTube
            item.Subtitle = ParseMetaTagAttributes(doc, "og:title", "content");
            item.ImageUrl = ParseMetaTagAttributes(doc, "og:image", "content");
            item.VideoUrl = ParseMetaTagAttributes(doc, "og:video:url", "content");
            item.VideoHeight = Convert.ToInt32(ParseMetaTagAttributes(doc, "og:video:height", "content"));
            item.VideoWidth = Convert.ToInt32(ParseMetaTagAttributes(doc, "og:video:width", "content"));
            item.MetaDescription = ParseMetaTagAttributes(doc, "og:description", "content");
            item.HostName = hostName;
            item.SiteName = ParseMetaTagAttributes(doc, "og:site_name", "content").ToLower();

            if (string.IsNullOrWhiteSpace(item.SiteName))
            {
                item.SiteName = item.HostName;
            }

            // There's no article text for YT, so just use the meta description
            item.ArticleText = $"<p>{item.MetaDescription}</p>";
        }

        private void SetGraphicMetaData(RssFeedItem item, string hostName)
        {
            // Extract the meta data from the Open Graph tags customized for YouTube
            item.ImageUrl = item.Url;
            item.HostName = hostName;
            item.SiteName = item.HostName;
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
