using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Antlr4.StringTemplate;
using Autofac;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RssFeeder.Console.ArticleDefinitions;
using RssFeeder.Console.Database;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;
using Serilog.Context;

namespace RssFeeder.Console
{
    public class WebCrawler : IWebCrawler
    {
        private readonly IRepository _crawlerRepository;
        private readonly IExportRepository _exportRepository;
        private readonly IArticleDefinitionFactory _definitions;
        private readonly IWebUtils _webUtils;
        private readonly IUtils _utils;
        private IContainer _container;

        private const string _exportCollectionName = "drudge-report";
        private const string _crawlerCollectionName = "feed-items";
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
$ArticleText$
" + MetaDataTemplate;

        private const string VideoTemplate = @"<iframe class=""{class}"" width=""$item.VideoWidth$"" height=""$item.VideoHeight$"" src=""$item.VideoUrl$"" frameborder=""0"" allow=""{allow}"" allowfullscreen></iframe>
<h3>$item.Subtitle$</h3>
$ArticleText$
" + MetaDataTemplate;

        private const string GraphicTemplate = @"<img src=""$item.ImageUrl$"" />
" + MetaDataTemplate;

        private const string BasicTemplate = @"<h3>$item.Title$</h3>
<p><a href=""$item.Url$"">Click here to read the full article</a></p>
" + MetaDataTemplate;

        public CrawlerConfig Config { get; set; }

        public WebCrawler(IRepository crawlerRepository, IExportRepository exportRepository, IArticleDefinitionFactory definitions, IWebUtils webUtils, IUtils utils)
        {
            _crawlerRepository = crawlerRepository;
            _exportRepository = exportRepository;
            _webUtils = webUtils;
            _utils = utils;
            _definitions = definitions;
        }

        public void Initialize(IContainer container)
        {
            Log.Information("Bootstrap initializing");
            Log.Information("Crawler exclusion list: {@exclusions}", Config.Exclusions);

            _container = container;

            if (_crawlerRepository != null)
                _crawlerRepository.EnsureDatabaseExists(_crawlerCollectionName, true);
            //if (_exportRepository != null)
            //    _exportRepository.EnsureDatabaseExists(_exportCollectionName, true);
        }

        public void Crawl(RssFeed feed)
        {
            // Create the working folder for the collection if it doesn't exist
            string workingFolder = Path.Combine(_utils.GetAssemblyDirectory(), feed.CollectionName);
            if (!Directory.Exists(workingFolder))
            {
                Log.Information("Creating folder '{workingFolder}'", workingFolder);
                Directory.CreateDirectory(workingFolder);
            }

            var list = GenerateList(feed, workingFolder);
            DownloadList(feed, workingFolder, list);
        }

        private void DownloadList(RssFeed feed, string workingFolder, List<RssFeedItem> list)
        {
            // Crawl any new articles and add them to the database
            Log.Information("Adding new articles to the {collectionName} collection", feed.CollectionName);
            int articleCount = 0;
            foreach (var item in list)
            {
                using (LogContext.PushProperty("url", item.FeedAttributes.Url))
                using (LogContext.PushProperty("urlHash", item.FeedAttributes.UrlHash))
                {
                    // No need to continue if we already crawled the article
                    if (_crawlerRepository.DocumentExists<RssFeedItem>(_crawlerCollectionName, feed.CollectionName, item.FeedAttributes.UrlHash))
                    {
                        continue;
                    }

                    // Increment new article count
                    Log.Information("UrlHash '{urlHash}' not found in collection '{collectionName}'", item.FeedAttributes.UrlHash, feed.CollectionName);
                    articleCount++;

                    try
                    {
                        // Construct unique file name
                        string friendlyHostname = item.FeedAttributes.Url.Replace("://", "_").Replace(".", "_");
                        int index = friendlyHostname.IndexOf("/");
                        if (index == -1)
                        {
                            friendlyHostname += "/";
                            index = friendlyHostname.IndexOf("/");
                        }

                        friendlyHostname = friendlyHostname.Substring(0, index);
                        string extension = GetFileExtension(new Uri(item.FeedAttributes.Url));
                        string filename = Path.Combine(workingFolder, $"{item.FeedAttributes.UrlHash}_{friendlyHostname}{extension}");

                        // Check for crawler exclusions, downloading content is blocked from these sites
                        Uri uri = new Uri(item.FeedAttributes.Url);
                        string hostName = uri.GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
                        if (Config.Exclusions.Contains(hostName))
                        {
                            Log.Information("Host '{hostName}' found on the exclusion list, skipping download", hostName);
                        }
                        else
                        {
                            // Download the Url contents, first using HttpClient but if that fails use Selenium
                            string newFilename = _webUtils.SaveUrlToDisk(item.Url, item.UrlHash, filename, !filename.Contains("_apnews_com") && !filename.Contains("_rumble_com"));
                            item.FeedAttributes.FileName = newFilename;
                            if (string.IsNullOrEmpty(newFilename) || newFilename.Contains("ajc_com") || newFilename.Contains("rumble_com"))
                            {
                                // Must have had an error on loading the url so attempt with Selenium
                                newFilename = _webUtils.WebDriverUrlToDisk(item.Url, item.UrlHash, newFilename);
                                item.FeedAttributes.FileName = newFilename;
                            }
                        }

                        // Parse the saved file as dictated by the site definitions
                        ParseArticleMetaTags(item, feed, _definitions);
                        if (string.IsNullOrEmpty(item.FeedAttributes.FileName))
{
                            _crawlerRepository.CreateDocument<RssFeedItem>(_crawlerCollectionName, item, feed.DatabaseRetentionDays, "", null, "");
                        }
                        else
                        {
                            using (var stream = new MemoryStream(File.ReadAllBytes(item.FeedAttributes.FileName)))
                            {
                                _crawlerRepository.CreateDocument<RssFeedItem>(_crawlerCollectionName, item, feed.DatabaseRetentionDays, Path.GetFileName(item.FeedAttributes.FileName), stream, "text/html");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "ERROR: Unable to save UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
                        articleCount--;
                    }
                }

                Log.Information("Added {count} new articles to the {collectionName} collection", articleCount, feed.CollectionName);
            }
        }

        private List<RssFeedItem> GenerateList(RssFeed feed, string workingFolder)
        {
            string html = _webUtils.DownloadString(feed.Url);

            // Save the feed html source for posterity
            string fileStem = Path.Combine(workingFolder, $"{DateTime.Now.ToUniversalTime():yyyyMMddhhmmss}_{feed.Url.Replace("://", "_").Replace(".", "_").Replace("/", "")}");
            _utils.SaveTextToDisk(html, fileStem + ".html", false);

            // Save thumbnail snapshot of the page
            if (feed.EnableThumbnail)
                _webUtils.SaveThumbnailToDisk(feed.Url, fileStem + ".png");

            // Parse the target links from the source to build the article crawl list
            var builder = _container.ResolveNamed<IRssFeedBuilder>(feed.CollectionName);
            var list = builder.GenerateRssFeedItemList(feed, html);

            return list;
        }

        private string GetFileExtension(Uri uri)
        {
            try
            {
                string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower();
                string query = uri.GetComponents(UriComponents.Query, UriFormat.Unescaped).ToLower();

                string extension = path.EndsWith(".png") ? ".png" :
                    path.EndsWith(".jpg") || path.EndsWith(".jpeg") || query.Contains("format=jpg") ? ".jpg" :
                    path.EndsWith(".gif") ? ".gif" :
                    ".html";

                return extension;
            }
            catch (UriFormatException ex)
            {
                Log.Error(ex, "GetComponents for {uri}", uri);
            }

            return ".html";
        }

        public void Export(RssFeed feed, DateTime startDate)
        {
            if (!feed.Exportable)
            {
                Log.Information("Feed {feedId} is not marked as exportable", feed.CollectionName);
                return;
            }

            // Get the articles from the source repository starting at the top of the hour
            var list = _crawlerRepository.GetExportDocuments<RssFeedItem>(_exportCollectionName, feed.CollectionName, startDate);

            // Loop through the list and upsert to the target repository
            foreach (var item in list)
            {
                // Replace the RavenDB primary key with a Guid for CosmosDB
                item.Id = Guid.NewGuid().ToString();

                Log.Information("EXPORT: UrlHash '{urlHash}' from {collectionName}", item.UrlHash, feed.CollectionName);
                _exportRepository.UpsertDocument<RssFeedItem>(_exportCollectionName, item);
            }

            Log.Information("Exported {count} new articles to the {collectionName} collection", list.Count, feed.CollectionName);
        }

        public void Purge(RssFeed feed)
        {
            // Purge stale files from working folder
            string workingFolder = Path.Combine(_utils.GetAssemblyDirectory(), feed.CollectionName);
            if (!Directory.Exists(workingFolder))
            {
                Log.Logger.Information("Folder '{workingFolder}' does not exist", workingFolder);
                return;
            }

            _utils.PurgeStaleFiles(workingFolder, feed.FileRetentionDays);

            // Purge stale documents from the database collection
            var list = _exportRepository.GetStaleDocuments<RssFeedItem>(_exportCollectionName, feed.CollectionName, feed.DatabaseRetentionDays);

            foreach (var item in list)
            {
                Log.Information("Removing UrlHash '{urlHash}' from {collectionName}", item.UrlHash, feed.CollectionName);
                _exportRepository.DeleteDocument<RssFeedItem>(_exportCollectionName, item.Id, item.HostName);
            }

            Log.Information("Removed {count} documents older than {maximumAgeInDays} days from {collectionName}", list.Count(), feed.DatabaseRetentionDays, feed.CollectionName);
        }

        private void ParseArticleMetaTags(RssFeedItem item, RssFeed feed, IArticleDefinitionFactory definitions)
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
                    item.Description = ApplyTemplateToDescription(item, feed, GraphicTemplate);
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
                            item.Description = ApplyTemplateToDescription(item, feed, VideoTemplate);
                        }
                        else
                        {
                            item.Description = ApplyTemplateToDescription(item, feed, ExtendedTemplate);
                        }
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
                Log.Information("No file to parse, applying basic metadata values for '{hostname}'", hostName);

                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(item, hostName);
                item.Description = ApplyTemplateToDescription(item, feed, BasicTemplate);
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

        private void SetExtendedArticleMetaData(RssFeedItem item, HtmlDocument doc, IArticleDefinitionFactory definitions, string hostName)
        {
            // Extract the meta data from the Open Graph tags helpfully provided with almost every article
            string url = item.Url;
            item.Url = ParseOpenGraphMetaTagAttributes(doc,  "og:url");

            if (string.IsNullOrWhiteSpace(item.Url) || hostName.Contains("frontpagemag.com"))
            {
                item.Url = url;
            }

            item.Subtitle = ParseOpenGraphMetaTagAttributes(doc,  "og:title");
            item.ImageUrl = ParseOpenGraphMetaTagAttributes(doc,  "og:image");
            item.HostName = hostName;
            item.SiteName = ParseOpenGraphMetaTagAttributes(doc,  "og:site_name").ToLower();

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
            var definition = definitions.Get(item.SiteName);

            using (LogContext.PushProperty("siteName", item.SiteName))
            {
                if (definition == null)
                {
                    Log.Information("No parsing definition found for '{siteName}' on hash '{urlHash}'", item.SiteName, item.UrlHash);

                    // If a specific article parser was not found in the database then
                    // use the fallback adaptive parser (experimental)
                    var parser = _container.ResolveNamed<IArticleParser>("adaptive-parser");
                    item.HtmlAttributes.Add("ArticleText", parser.GetArticleBySelector(doc.Text, definition));
                }
                else
                {
                    // Resolve the parser defined for the site
                    var parser = _container.ResolveNamed<IArticleParser>(definition.Parser);
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
            item.Subtitle = ParseOpenGraphMetaTagAttributes(doc,  "og:title");
            item.ImageUrl = ParseOpenGraphMetaTagAttributes(doc,  "og:image");
            item.HostName = hostName;
            item.SiteName = ParseOpenGraphMetaTagAttributes(doc,  "og:site_name").ToLower();

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
                item.VideoUrl = ParseOpenGraphMetaTagAttributes(doc,  "og:video:url");
                item.VideoHeight = int.TryParse(ParseOpenGraphMetaTagAttributes(doc,  "og:video:height"), out int height) ? height : 0;
                item.VideoWidth = int.TryParse(ParseOpenGraphMetaTagAttributes(doc,  "og:video:width"), out int width) ? width : 0;
            }
            Log.Information("Video URL: '{url}' ({height}x{width})", item.VideoUrl, item.VideoHeight, item.VideoWidth);

            // There's no article text for most video sites, so just use the meta description
            var description = ParseOpenGraphMetaTagAttributes(doc,  "og:description");
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
                Log.Information("Meta attribute '{attribute}':'{property}' has a value of '{value}'", targetAttributeName, targetAttributeValue, sourceAttributeValue);
            }

            return sourceAttributeValue;
        }
    }
}
