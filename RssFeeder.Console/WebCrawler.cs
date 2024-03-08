namespace RssFeeder.Console;

public class WebCrawler : IWebCrawler
{
    private readonly IRepository _crawlerRepository;
    private readonly IExportRepository _exportRepository;
    private readonly IArticleParser _articleParser;
    private readonly IArticleDefinitionFactory _definitions;
    private readonly IArticleExporter _exporter;
    private readonly IWebUtils _webUtils;
    private readonly IUtils _utils;
    private readonly ILogger _log;
    private string _workingFolder;
    private IContainer _container;

    private string _exportCollectionName = "drudge-report";
    private string _crawlerCollectionName = "feed-items";

    public WebCrawler(IRepository crawlerRepository, IExportRepository exportRepository, IArticleDefinitionFactory definitions,
        IWebUtils webUtils, IUtils utils, IArticleParser articleParser, IArticleExporter exporter, ILogger log)
    {
        _crawlerRepository = crawlerRepository;
        _exportRepository = exportRepository;
        _webUtils = webUtils;
        _utils = utils;
        _definitions = definitions;
        _articleParser = articleParser;
        _exporter = exporter;
        _log = log;
    }

    public void Initialize(IContainer container, string crawlerCollectionName, string exportCollectionName)
    {
        _log.Information($"{nameof(WebCrawler)} initializing");

        _container = container;
        _crawlerCollectionName = crawlerCollectionName;
        _exportCollectionName = exportCollectionName;

        if (_crawlerRepository != null)
            _crawlerRepository.EnsureDatabaseExists(_crawlerCollectionName, true);
        if (_exportRepository != null)
            _exportRepository.EnsureDatabaseExists(_exportCollectionName, true);
    }

    public void Crawl(Guid runID, RssFeed feed)
    {
        _workingFolder = PrepareWorkspace(feed);
        var list = GenerateFeedLinks(feed);
        DownloadList(runID, feed, list);
    }

    private string PrepareWorkspace(RssFeed feed)
    {
        // Create the working folder for the collection if it doesn't exist
        string workingFolder = Path.Combine(_utils.GetAssemblyDirectory(), feed.CollectionName);
        if (!Directory.Exists(workingFolder))
        {
            _log.Information("Creating folder '{workingFolder}'", workingFolder);
            Directory.CreateDirectory(workingFolder);
        }

        return workingFolder;
    }

    private bool TryParseAndSave(RssFeed feed, RssFeedItem item)
    {
        var uri = new Uri(item.HtmlAttributes.GetValueOrDefault("Url") ?? item.FeedAttributes.Url);
        if (uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query))
        {
            _log.Information("URI '{uri}' detected as a home page rather than an article, skipping parse operation", uri);
            return false;
        }

        try
        {
            // Parse the downloaded file as dictated by the site parsing definitions
            _articleParser.Parse(item);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "PARSE_ERROR: UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
            return false;
        }

        try
        {

            _crawlerRepository.SaveDocument<RssFeedItem>(_crawlerCollectionName, item, feed.DatabaseRetentionDays, "", null, "");

            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "SAVE_ERROR: UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
        }

        return false;
    }

    private void DownloadList(Guid runID, RssFeed feed, List<RssFeedItem> list)
    {
        _articleParser.Initialize(_container, _definitions, _webUtils, _log);

        // Crawl any new articles and add them to the database
        _log.Information("Downloading new articles to the {collectionName} collection", feed.CollectionName);
        int articleCount = 0;
        foreach (var item in list)
        {
            //using (LogContext.PushProperty("variation", variation))
            using (LogContext.PushProperty("url", item.FeedAttributes.Url))
            using (LogContext.PushProperty("urlHash", item.FeedAttributes.UrlHash))
            {
                // Throttle any articles beyond the headlines
                if (articleCount >= 25 && !item.FeedAttributes.IsHeadline)
                {
                    _log.Debug("Throttling engaged");
                    break;
                }

                // No need to continue if we already crawled the article
                if (_crawlerRepository.DocumentExists<RssFeedItem>(_crawlerCollectionName, feed.CollectionName, item.FeedAttributes.UrlHash))
                {
                    _log.Debug("UrlHash '{urlHash}' already exists in collection '{collectionName}'", item.FeedAttributes.UrlHash, feed.CollectionName);
                    continue;
                }

                item.RunId = runID;
                _log.Information("BEGIN: UrlHash {urlHash}|{linkLocation}|{title}", item.FeedAttributes.UrlHash, item.FeedAttributes.LinkLocation, item.FeedAttributes.Title);

                try
                {
                    CrawlItem(item);

                    if (TryParseAndSave(feed, item))
                        articleCount++;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "DOWNLOAD_ERROR: UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
                }

                _log.Information("END: UrlHash {urlHash}", item.FeedAttributes.UrlHash);
            }
        }

        _log.Information("Downloaded {count} new articles to the {collectionName} collection", articleCount, feed.CollectionName);
    }

    private void CrawlItem(RssFeedItem item)
    {
        var sourceUri = new Uri(item.FeedAttributes.Url);
        string hostname = sourceUri.Host.ToLower();
        object content = default;

        // Crawl the given uri
        if (!CanCrawl(hostname, sourceUri))
            return;

        // Issue a HEAD request to determine the content type and unshortened Uri
        (HttpStatusCode statusCode, Uri trueUri, string contentType) = _webUtils.GetContentType(item.FeedAttributes.Url);

        // First chance to reset the hostname from the true uri
        hostname = trueUri?.Host.ToLower() ?? hostname;

        if (IsBinary(contentType))
        {
            content = _webUtils.ClientGetBytes(trueUri?.AbsoluteUri ?? sourceUri.AbsoluteUri);
            statusCode = HttpStatusCode.OK;
        }
        else if ((statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.MethodNotAllowed) && !_crawlerRepository.Config.WebDriver.Contains(hostname))
        {
            // Download the url contents using RestSharp
            (statusCode, content, trueUri, contentType) = _webUtils.ClientGetString(trueUri?.AbsoluteUri ?? sourceUri.AbsoluteUri);
        }

        // Switch to Selenium web driver based on certain failed responses from RestSharp
        bool crawlWithSelenium =
            (statusCode == HttpStatusCode.Found ||
            statusCode == HttpStatusCode.MovedPermanently ||
            statusCode == HttpStatusCode.PermanentRedirect ||
            statusCode == HttpStatusCode.Redirect ||
            statusCode == HttpStatusCode.NotAcceptable ||
            (int)statusCode == 0) && CanCrawl(hostname, trueUri ?? sourceUri) && !IsBinary(contentType);

        // Attempt crawl with Selenium web driver (only works on text content)
        if (crawlWithSelenium || _crawlerRepository.Config.WebDriver.Contains(hostname))
        {
            (_, content, trueUri, contentType) = _webUtils.DriverGetString(trueUri?.AbsoluteUri ?? sourceUri.AbsoluteUri);
        }

        // Last chance to reset the hostname from the true uri
        hostname = trueUri?.Host.ToLower() ?? hostname;
        if (string.IsNullOrEmpty(item.HostName)) item.HostName = hostname;
        if (string.IsNullOrEmpty(item.SiteName)) item.SiteName = hostname;
        item.HtmlAttributes.Add("Url", trueUri?.AbsoluteUri ?? sourceUri.AbsoluteUri);

        // Construct unique file name
        string contentTypeExtension = crawlWithSelenium ? GetFileExtensionByPathQuery(trueUri ?? sourceUri) : GetFileExtensionByContentType(contentType);
        string friendlyHostname = hostname.Replace(".", "_");
        string filename = Path.Combine(_workingFolder, $"{item.FeedAttributes.UrlHash}_{friendlyHostname}{contentTypeExtension}");

        switch (content)
        {
            case string:
                _webUtils.SaveContentToDisk(filename, !_crawlerRepository.Config.IncludeScripts.Contains(hostname), (string)content);
                item.FeedAttributes.FileName = filename;
                break;
            case byte[]:
                _webUtils.SaveContentToDisk(filename, (byte[])content);
                item.FeedAttributes.FileName = filename;
                break;
            default:
                _log.Debug("No content downloaded");
                break;
        }
    }

    private bool IsBinary(string contentType)
    {
        string contentTypeLowered = contentType?.ToLower() ?? "text/html";

        switch (contentTypeLowered)
        {
            case "text/html":
            case "text/plain":
            case "text/json":
            case "text/xml":
            case "application/json":
            case "application/xhtml+xml":
            case "application/xml":
                return false;
            case "image/jpg":
            case "image/jpeg":
            case "application/jpg":
            case "image/gif":
            case "image/png":
            case "image/webp":
            case "application/pdf":
            case "audio/mpeg":
            case "video/mp4":
                return true;
            default:
                _log.Warning("Unexpected content type {contentType}", contentTypeLowered);
                return false;
        }
    }

    private bool CanCrawl(string hostname, Uri uri)
    {
        // Check for crawler exclusions, downloading content is blocked from these sites
        if (_crawlerRepository.Config.Exclusions.Contains(hostname))
        {
            _log.Information("Host '{hostName}' found on the exclusion list, skipping download", hostname);
            return false;
        }

        if (uri is null)
            return false;

        if (uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query))
        {
            _log.Information("URI '{uri}' detected as a home page rather than an article, skipping download", uri);
            return false;
        }

        return true;
    }

    private List<RssFeedItem> GenerateFeedLinks(RssFeed feed)
    {
        (_, string html, _, _) = feed.RequiresJavascript ? _webUtils.DriverGetString(feed.Url) : _webUtils.DownloadString(feed.Url);

        // Build the file stem so we can save the html source and a screenshot of the feed page
        var uri = new Uri(feed.Url);
        string hostname = uri.Host.ToLower();
        string friendlyHostname = hostname.Replace(".", "_");
        string fileStem = Path.Combine(_workingFolder, $"{DateTime.Now.ToUniversalTime():yyyyMMddhhmmss}_{friendlyHostname}");

        // Save the feed html source for posterity
        _utils.SaveTextToDisk(html, fileStem + ".html", false);

        // Save thumbnail snapshot of the page
        if (feed.EnableThumbnail)
            _webUtils.SaveThumbnailToDisk(feed.Url, fileStem + ".png");

        // Parse the target links from the source to build the article crawl list
        var builder = _container.ResolveNamed<IRssFeedBuilder>(feed.CollectionName);
        var list = builder.GenerateRssFeedItemList(feed, html);

        return list;
    }

    private string GetFileExtensionByPathQuery(Uri uri)
    {
        try
        {
            string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower();
            string query = uri.GetComponents(UriComponents.Query, UriFormat.Unescaped).ToLower();

            string extension = path.EndsWith(".png") || query.Contains("format=png") ? ".png" :
                path.EndsWith(".jpg") || path.EndsWith(".jpeg") || query.Contains("format=jpg") ? ".jpg" :
                path.EndsWith(".gif") || query.Contains("format=gif") ? ".gif" :
                path.EndsWith(".pdf") || query.Contains("format=pdf") ? ".pdf" :
                ".html";

            _log.Information("GetFileExtensionByPathQuery: Detected extension {extension} from path /{path}?{query}", extension, path, query);
            return extension;
        }
        catch (UriFormatException ex)
        {
            _log.Error(ex, "GetFileExtensionByPathQuery: Error for {uri}", uri);
        }

        return ".html";
    }

    private string GetFileExtensionByContentType(string contentType)
    {
        string extension = ".html";
        string contentTypeLowered = contentType?.ToLower() ?? "text/html";

        switch (contentTypeLowered)
        {
            case "text/html":
            case "application/xhtml+xml":
                extension = ".html";
                break;
            case "text/plain":
                extension = ".txt";
                break;
            case "image/jpg":
            case "image/jpeg":
            case "application/jpg":
                extension = ".jpg";
                break;
            case "image/gif":
                extension = ".gif";
                break;
            case "image/png":
                extension = ".png";
                break;
            case "application/json":
            case "text/json":
                extension = ".json";
                break;
            case "application/pdf":
                extension = ".pdf";
                break;
            case "application/xml":
                extension = ".xml";
                break;
            case "audio/mpeg":
                extension = ".mp3";
                break;
            case "video/mp4":
                extension = ".mp4";
                break;
            case "image/webp":
                extension = ".webp";
                break;
        }

        _log.Information("GetFileExtensionByContentType: Detected extension {extension} from content type {contentType}", extension, contentTypeLowered);
        return extension;
    }

    public void Export(Guid runID, RssFeed feed, DateTime startDate)
    {
        if (!feed.Exportable)
        {
            _log.Information("Feed {feedId} is not marked as exportable", feed.CollectionName);
            return;
        }

        // Get the articles from the source repository starting at the top of the hour
        var list = _crawlerRepository.GetExportDocuments<RssFeedItem>(_crawlerCollectionName, feed.CollectionName, runID);

        // Loop through the list and upsert to the target repository
        foreach (var item in list)
        {
            using (LogContext.PushProperty("runID", runID))
            using (LogContext.PushProperty("url", item.FeedAttributes.Url))
            using (LogContext.PushProperty("urlHash", item.FeedAttributes.UrlHash))
            {
                _log.Debug("Preparing '{urlHash}' for export", item.FeedAttributes.UrlHash);
                var exportFeedItem = _exporter.FormatItem(item, feed);

                _log.Information("EXPORT: UrlHash '{urlHash}' from {collectionName}", item.FeedAttributes.UrlHash, feed.CollectionName);
                _exportRepository.UpsertDocument<ExportFeedItem>(_exportCollectionName, exportFeedItem);
            }
        }

        _log.Information("Exported {count} new articles to the {collectionName} collection", list.Count, feed.CollectionName);
    }

    public void Purge(RssFeed feed)
    {
        // Purge stale files from working folder
        string workingFolder = Path.Combine(_utils.GetAssemblyDirectory(), feed.CollectionName);
        if (!Directory.Exists(workingFolder))
        {
            _log.Warning("Folder '{workingFolder}' does not exist", workingFolder);
            return;
        }

        _utils.PurgeStaleFiles(workingFolder, feed.FileRetentionDays);

        // Purge stale documents from the database collection
        var list = _crawlerRepository.GetStaleDocuments<RssFeedItem>(_crawlerCollectionName, feed.CollectionName, 7);
        _log.Information("Stripped {count} documents older than 7 days from {collectionName}", list.Count, feed.CollectionName);

        foreach (var item in list)
        {
            _log.Debug("Stripping UrlHash '{urlHash}' from {collectionName}", item.FeedAttributes.UrlHash, feed.CollectionName);
            item.OpenGraphAttributes = default;
            item.HtmlAttributes = default;
            _crawlerRepository.SaveDocument<RssFeedItem>(_crawlerCollectionName, item, feed.DatabaseRetentionDays);
        }
    }
}
