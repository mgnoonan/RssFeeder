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
    private IContainer _container;

    private string _exportCollectionName = "drudge-report";
    private string _crawlerCollectionName = "feed-items";

    public CrawlerConfig Config { get; set; }

    public WebCrawler(IRepository crawlerRepository, IExportRepository exportRepository, IArticleDefinitionFactory definitions,
        IWebUtils webUtils, IUtils utils, IArticleParser articleParser, IArticleExporter exporter)
    {
        _crawlerRepository = crawlerRepository;
        _exportRepository = exportRepository;
        _webUtils = webUtils;
        _utils = utils;
        _definitions = definitions;
        _articleParser = articleParser;
        _exporter = exporter;
    }

    public void Initialize(IContainer container, string crawlerCollectionName, string exportCollectionName)
    {
        Log.Information($"{nameof(WebCrawler)} initializing");
        Log.Information("Crawler exclusion list: {@exclusions}", Config.Exclusions);

        _container = container;
        _crawlerCollectionName = crawlerCollectionName;
        _exportCollectionName = exportCollectionName;

        if (_crawlerRepository != null)
            _crawlerRepository.EnsureDatabaseExists(_crawlerCollectionName, true);
        if (_exportRepository != null)
            _exportRepository.EnsureDatabaseExists(_exportCollectionName, true);
    }

    public void Crawl(RssFeed feed)
    {
        string workingFolder = PrepareWorkspace(feed);
        var list = GenerateFeedLinks(feed, workingFolder);
        DownloadList(feed, workingFolder, list);
        //ParseAndSave(feed, list);
    }

    private string PrepareWorkspace(RssFeed feed)
    {
        // Create the working folder for the collection if it doesn't exist
        string workingFolder = Path.Combine(_utils.GetAssemblyDirectory(), feed.CollectionName);
        if (!Directory.Exists(workingFolder))
        {
            Log.Information("Creating folder '{workingFolder}'", workingFolder);
            Directory.CreateDirectory(workingFolder);
        }

        return workingFolder;
    }

    private void ParseAndSave(RssFeed feed, List<RssFeedItem> list)
    {
        _articleParser.Initialize(_container, _definitions, _webUtils);

        foreach (var item in list)
        {
            using (LogContext.PushProperty("url", item.FeedAttributes.Url))
            using (LogContext.PushProperty("urlHash", item.FeedAttributes.UrlHash))
            {
                try
                {
                    // Parse the downloaded file as dictated by the site parsing definitions
                    _articleParser.Parse(item);

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
                    Log.Error(ex, "PARSE_ERROR: UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
                }
            }
        }
    }

    private void ParseAndSave(RssFeed feed, RssFeedItem item)
    {
        var uri = new Uri(item.HtmlAttributes.GetValueOrDefault("Url") ?? item.FeedAttributes.Url);
        if (uri.AbsolutePath == "/")
        {
            Log.Information("URI '{uri}' detected as a home page rather than an article, skipping parse operation", uri);
            return;
        }

        try
        {
            // Parse the downloaded file as dictated by the site parsing definitions
            _articleParser.Parse(item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PARSE_ERROR: UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
        }

        try
        {

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
            Log.Error(ex, "SAVE_ERROR: UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
        }
    }

    private void DownloadList(RssFeed feed, string workingFolder, List<RssFeedItem> list)
    {
        _articleParser.Initialize(_container, _definitions, _webUtils);

        // Crawl any new articles and add them to the database
        Log.Information("Downloading new articles to the {collectionName} collection", feed.CollectionName);
        int articleCount = 0;
        foreach (var item in list)
        {
            using (LogContext.PushProperty("url", item.FeedAttributes.Url))
            using (LogContext.PushProperty("urlHash", item.FeedAttributes.UrlHash))
            {
                // No need to continue if we already crawled the article
                if (_crawlerRepository.DocumentExists<RssFeedItem>(_crawlerCollectionName, feed.CollectionName, item.FeedAttributes.UrlHash))
                {
                    Log.Information("UrlHash '{urlHash}' already exists in collection '{collectionName}'", item.FeedAttributes.UrlHash, feed.CollectionName);
                    continue;
                }

                // Increment new article count
                Log.Information("UrlHash '{urlHash}' not found in collection '{collectionName}'", item.FeedAttributes.UrlHash, feed.CollectionName);
                articleCount++;

                try
                {
                    // Construct unique file name
                    var uri = new Uri(item.FeedAttributes.Url);
                    string hostname = uri.Host.ToLower();
                    string friendlyHostname = hostname.Replace(".", "_");
                    string extension = GetFileExtension(uri);
                    string filename = Path.Combine(workingFolder, $"{item.FeedAttributes.UrlHash}_{friendlyHostname}{extension}");

                    // Check for crawler exclusions, downloading content is blocked from these sites
                    if (Config.Exclusions.Contains(hostname))
                    {
                        Log.Information("Host '{hostName}' found on the exclusion list, skipping download", hostname);
                    }
                    else if (uri.AbsolutePath == "/")
                    {
                        Log.Information("URI '{uri}' detected as a home page rather than an article, skipping download", uri);
                    }
                    else
                    {
                        // Download the Url contents, first using HttpClient but if that fails use Selenium
                        (string newFilename, Uri trueUri) = _webUtils.SaveUrlToDisk(item.FeedAttributes.Url, item.FeedAttributes.UrlHash, filename);
                        item.FeedAttributes.FileName = newFilename;
                        item.HtmlAttributes.Add("Url", trueUri.AbsoluteUri);

                        // Must have had an error on loading the url so attempt with Selenium
                        if (string.IsNullOrEmpty(newFilename) || newFilename.Contains("ajc_com"))
                        {
                            (newFilename, trueUri) = _webUtils.WebDriverUrlToDisk(item.FeedAttributes.Url, item.FeedAttributes.UrlHash, filename);
                            item.FeedAttributes.FileName = newFilename;
                            item.HtmlAttributes["Url"] = trueUri.AbsoluteUri;
                        }
                    }

                    // Parse the saved file as dictated by the site definitions
                    ParseAndSave(feed, item);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "DOWNLOAD_ERROR: UrlHash '{urlHash}':'{url}'", item.FeedAttributes.UrlHash, item.FeedAttributes.Url);
                    articleCount--;
                }
            }
        }

        Log.Information("Downloaded {count} new articles to the {collectionName} collection", articleCount, feed.CollectionName);
    }

    private List<RssFeedItem> GenerateFeedLinks(RssFeed feed, string workingFolder)
    {
        (string html, Uri trueUri) = _webUtils.DownloadString(feed.Url);

        // Build the file stem so we can save the html source and a screenshot of the feed page
        var uri = new Uri(feed.Url);
        string hostname = uri.Host.ToLower();
        string friendlyHostname = hostname.Replace(".", "_");
        string fileStem = Path.Combine(workingFolder, $"{DateTime.Now.ToUniversalTime():yyyyMMddhhmmss}_{friendlyHostname}");

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
        var list = _crawlerRepository.GetExportDocuments<RssFeedItem>(_crawlerCollectionName, feed.CollectionName, startDate);

        // Loop through the list and upsert to the target repository
        foreach (var item in list)
        {
            using (LogContext.PushProperty("url", item.FeedAttributes.Url))
            using (LogContext.PushProperty("urlHash", item.FeedAttributes.UrlHash))
            {

                var exportFeedItem = _exporter.FormatItem(item, feed);

                Log.Information("EXPORT: UrlHash '{urlHash}' from {collectionName}", item.FeedAttributes.UrlHash, feed.CollectionName);
                _exportRepository.UpsertDocument<ExportFeedItem>(_exportCollectionName, exportFeedItem);
            }
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
        var list = _exportRepository.GetStaleDocuments<ExportFeedItem>(_exportCollectionName, feed.CollectionName, feed.DatabaseRetentionDays);

        foreach (var item in list)
        {
            Log.Information("Removing UrlHash '{urlHash}' from {collectionName}", item.UrlHash, feed.CollectionName);
            _exportRepository.DeleteDocument<ExportFeedItem>(_exportCollectionName, item.Id, item.HostName);
        }

        Log.Information("Removed {count} documents older than {maximumAgeInDays} days from {collectionName}", list.Count, feed.DatabaseRetentionDays, feed.CollectionName);
    }
}
