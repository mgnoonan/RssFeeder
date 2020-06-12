using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Xml;
using Antlr3.ST;
using CommandLine;
using HtmlAgilityPack;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using RssFeeder.Console.CustomBuilders;
using RssFeeder.Console.Parsers;
using RssFeeder.Models;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace RssFeeder.Console
{
    /// <summary>
    /// A utility to retrieve RSS Feeds from remote servers
    /// </summary>
    class Program
    {
        private const string DATEFORMAT = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

        /// <summary>
        /// Name of the working area folder
        /// </summary>
        private const string WORKING_FOLDER = "working";

        private const string MetaDataTemplate = @"
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
        /// The Azure DocumentDB endpoint for running this GetStarted sample.
        /// </summary>
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUrl"];

        /// <summary>
        /// The primary key for the Azure DocumentDB account.
        /// </summary>
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        private static ILogger log;

        /// <summary>
        /// The DocumentDB client instance.
        /// </summary>
        private static DocumentClient _client = null;

        private static DocumentClient CosmosClient { get => _client ?? new DocumentClient(new Uri(EndpointUri), PrimaryKey); }

        /// <summary>
        /// The list of site definitions that describe how to get an article
        /// </summary>
        private static List<SiteArticleDefinition> ArticleDefinitions;

        /// <summary>
        /// Cache for any site parsers created through reflection
        /// </summary>
        private static Dictionary<string, IArticleParser> ArticleParserCache = new Dictionary<string, IArticleParser>();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Grab the current assembly name
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            // Init Serilog
            log = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(new RenderedCompactJsonFormatter(), "RssFeeder.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();
            Log.Logger = log;

            log.Information("START: Machine: {machineName} Assembly: {assembly}", Environment.MachineName, assemblyName.FullName);

            // set up TLS defaults
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Process the command line arguments
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => ProcessWithExitCode(opts))
                .WithNotParsed<Options>(errs => HandleParserError(errs, args))
                ;
        }

        private static void TestArticleDefinition(SiteArticleDefinition definition)
        {
            string html;
            IArticleParser parser;

            if (!string.IsNullOrEmpty(definition.TestArticleUrl))
            {
                // set up TLS defaults
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                html = GetResponse(definition.TestArticleUrl);
            }
            else
            {
                string workingFolder = Path.Combine(AssemblyDirectory, WORKING_FOLDER);
                html = File.ReadAllText(Path.Combine(workingFolder.Replace("\\Debug\\", "\\Release\\"), definition.TestArticleFilename));
            }

            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));
            var item = new RssFeedItem { Url = "http://www.test.com" };
            SetExtendedArticleMetaData(item, doc);

            Type type = Assembly.GetExecutingAssembly().GetType(definition.Parser);
            parser = (IArticleParser)Activator.CreateInstance(type);

            // For console display, strip out the paragraph tags
            string text = parser.GetArticleBySelector(html, definition)
                .Replace("<p>", "")
                .Replace("</p>", "\n");

            System.Console.WriteLine(JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented));
            System.Console.WriteLine(text);
        }

        private static void TestFeedDefinition(SiteArticleDefinition definition)
        {
            string workingFolder = Path.Combine(AssemblyDirectory, WORKING_FOLDER);
            string html = File.ReadAllText(Path.Combine(workingFolder.Replace("\\Debug\\", "\\Release\\"), definition.TestFeedFilename));

            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));

            Type type = Assembly.GetExecutingAssembly().GetType("RssFeeder.Console.CustomBuilders.DrudgeReportFeedBuilder");
            var parser = (IRssFeedBuilder)Activator.CreateInstance(type);

            parser.ParseRssFeedItems(Log.Logger, html, new List<string> { "fb27ce207f3ca32d97999d182ec93576", "0cc6fcfe73c643623766047524ab10e5" });
        }

        static void HandleParserError(IEnumerable<CommandLine.Error> errors, string[] args)
        {
            // Return an error code
            log.Error("Invalid arguments: '{@args}'", args);
            Environment.Exit(255);
        }

        static void ProcessWithExitCode(Options opts)
        {
            // Zero return value means everything processed normally
            int returnCode = 0;

            try
            {
                // A config file was specified so read in the options from there
                List<Options> optionsList;
                if (!string.IsNullOrWhiteSpace(opts.TestDefinition))
                {
                    optionsList = new List<Options> { opts };
                }
                else if (!string.IsNullOrWhiteSpace(opts.Config))
                {
                    // Get the directory of the current executable, all config 
                    // files should be in this path
                    string configFile = Path.Combine(AssemblyDirectory, opts.Config);
                    log.Information("Reading from config file: {configFile}", configFile);

                    using (StreamReader sr = new StreamReader(configFile))
                    {
                        // Read the options in JSON format
                        string json = sr.ReadToEnd();
                        log.Information("Options: {@options}", json);

                        // Deserialize into our options class
                        optionsList = JsonConvert.DeserializeObject<List<Options>>(json);
                    }
                }
                else
                {
                    string databaseName = "rssfeeder";
                    string collectionName = "feeds";
                    optionsList = GetAllDocuments<Options>(databaseName, collectionName);
                }

                foreach (var option in optionsList)
                {
                    if (!string.IsNullOrWhiteSpace(option.TestDefinition))
                    {
                        using (StreamReader sr = new StreamReader(option.TestDefinition))
                        {
                            // Read the options in JSON format
                            string json = sr.ReadToEnd();
                            log.Information("Test configuration: {@options}", json);

                            // Deserialize into our options class
                            var definition = JsonConvert.DeserializeObject<SiteArticleDefinition>(json);

                            if (string.IsNullOrWhiteSpace(definition.TestArticleFilename) && string.IsNullOrWhiteSpace(definition.TestArticleUrl))
                            {
                                TestFeedDefinition(definition);
                            }
                            else
                            {
                                TestArticleDefinition(definition);
                            }
                        }
                    }
                    else
                    {
                        // Transform the option to the old style feed
                        var f = new RssFeed
                        {
                            Title = option.Title,
                            Description = option.Description,
                            OutputFile = option.OutputFile,
                            Language = option.Language,
                            Url = option.Url,
                            CustomParser = option.CustomParser,
                            Filters = option.Filters.ToList()
                        };

                        var items = BuildFeedLinks(f);
                        BuildRssFileUsingTemplate(f, items.OrderByDescending(i => i.DateAdded).ToList());
                    }
                }

                log.Information("END: Completed successfully");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error during processing");
                returnCode = 250;
            }
            finally
            {
                Log.CloseAndFlush();
            }

            if (Environment.UserInteractive)
            {
                System.Console.WriteLine("\nPress <Enter> to continue...");
                System.Console.ReadLine();
            }

            Environment.Exit(returnCode);
        }

        public static string GetResponse(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

            string source = String.Empty;

            using (WebResponse webResponse = req.GetResponse())
            {
                using (HttpWebResponse httpWebResponse = webResponse as HttpWebResponse)
                {
                    StreamReader reader;
                    if (httpWebResponse.ContentEncoding.ToLower().Contains("gzip"))
                    {
                        reader = new StreamReader(new GZipStream(httpWebResponse.GetResponseStream(), CompressionMode.Decompress));
                    }
                    else if (httpWebResponse.ContentEncoding.ToLower().Contains("deflate"))
                    {
                        reader = new StreamReader(new DeflateStream(httpWebResponse.GetResponseStream(), CompressionMode.Decompress));
                    }
                    else
                    {
                        reader = new StreamReader(httpWebResponse.GetResponseStream());
                    }
                    source = reader.ReadToEnd();
                }
            }

            req.Abort();

            return source;
        }

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);

                // Add the trailing backslash if not present
                string name = Path.GetDirectoryName(path);
                if (!name.EndsWith("\\"))
                    name += "\\";

                return name;
            }
        }

        private static List<RssFeedItem> BuildFeedLinks(RssFeed feed)
        {
            string builderName = feed.CustomParser;
            if (string.IsNullOrWhiteSpace(builderName))
                return new List<RssFeedItem>();

            Type type = Assembly.GetExecutingAssembly().GetType(builderName);
            IRssFeedBuilder builder = (IRssFeedBuilder)Activator.CreateInstance(type);
            var list = builder.ParseRssFeedItems(log, feed, out string html)
                //.Take(10) FOR DEBUG PURPOSES
                ;

            string databaseName = "rssfeeder";
            string collectionName = "drudge-report";

            // Load the collection of site parsers
            ArticleDefinitions = GetAllDocuments<SiteArticleDefinition>(databaseName, "site-parsers");

            // Create the working folder if it doesn't exist
            string workingFolder = Path.Combine(AssemblyDirectory, WORKING_FOLDER);
            if (!Directory.Exists(workingFolder))
            {
                log.Information("Creating folder '{workingFolder}'", workingFolder);
                Directory.CreateDirectory(workingFolder);
            }

            // Save the feed source for posterity
            string feedSource = Path.Combine(workingFolder, $"{DateTime.Now.ToUniversalTime().ToString("yyyyMMddhhmmss")}_{feed.Url.Replace("://", "_").Replace(".", "_")}.html");
            SaveTextToDisk(html, feedSource, false);

            // Add any links that don't already exist
            log.Information("Adding links to the database");
            foreach (var item in list)
            {
                if (!DocumentExists(databaseName, collectionName, item))
                {
                    SaveUrlToDisk(item, workingFolder);
                    ParseArticleMetaTags(item);
                    CreateDocument(databaseName, collectionName, item);
                }
            }

            // Remove any stale documents
            log.Information("Removing stale links from {databaseName}", databaseName);
            list = GetStaleDocuments(databaseName, collectionName, 7);
            foreach (var item in list)
            {
                log.Information("Removing UrlHash '{urlHash}'", item.UrlHash);
                DeleteDocument(databaseName, collectionName, item.Id);
            }

            // Purge stale files from working folder
            log.Information("Removing stale files from {workingFolder}", workingFolder);
            PurgeStaleFiles(workingFolder, 7);

            // Return whatever documents are left in the database
            return GetAllDocuments<RssFeedItem>(databaseName, collectionName);
        }

        public static void PurgeStaleFiles(string folderPath, short maximumAgeInDays)
        {
            DateTime minimumDate = DateTime.Now.AddDays(-maximumAgeInDays);

            var files = Directory.EnumerateFiles(folderPath);

            foreach (var file in files)
            {
                DeleteFileIfOlderThan(file, minimumDate);
            }
        }

        private static void DeleteFileIfOlderThan(string path, DateTime date)
        {
            var file = new FileInfo(path);
            if (file.CreationTime < date)
            {
                log.Information("Removing {fileName}", file.FullName);
                file.Delete();
            }
        }

        /// <summary>
        /// Create the Family document in the collection if another by the same partition key/item key doesn't already exist.
        /// </summary>
        /// <param name="databaseName">The name/ID of the database.</param>
        /// <param name="collectionName">The name/ID of the collection.</param>
        /// <param name="item"></param>
        private static void CreateDocument(string databaseName, string collectionName, RssFeedItem item)
        {
            var result = CosmosClient.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), item)
                .Result;

            if (result.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Unable to create document for '{item.UrlHash}'");
            }
        }

        private static List<RssFeedItem> GetStaleDocuments(string databaseName, string collectionName, short maximumAgeInDays)
        {
            DateTime targetDate = DateTime.Now.AddDays(-maximumAgeInDays);

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            return CosmosClient.CreateDocumentQuery<RssFeedItem>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .Where(f => f.DateAdded <= targetDate)
                .ToList();
        }

        private static bool DocumentExists(string databaseName, string collectionName, RssFeedItem item)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            var result = CosmosClient.CreateDocumentQuery<RssFeedItem>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .Where(f => f.UrlHash == item.UrlHash);

            return result.Count() > 0;
        }

        private static List<T> GetAllDocuments<T>(string databaseName, string collectionName)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            return CosmosClient.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .ToList();
        }

        private static void DeleteDocument(string databaseName, string collectionName, RssFeedItem item)
        {
            DeleteDocument(databaseName, collectionName, item.Id);
        }

        private static void DeleteDocument(string databaseName, string collectionName, string documentID)
        {
            var result = CosmosClient.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentID)).Result;

            if (result.StatusCode != HttpStatusCode.NoContent)
            {
                throw new Exception($"Unable to delete document for '{documentID}'");
            }
        }

        private static void ParseArticleMetaTags(RssFeedItem item)
        {
            if (File.Exists(item.FileName))
            {
                // Article was successfully downloaded from the target site
                log.Information("Parsing meta tags from file '{fileName}'", item.FileName);

                var doc = new HtmlDocument();
                doc.Load(item.FileName);

                if (!doc.DocumentNode.HasChildNodes)
                {
                    log.Warning("No file content found, skipping.");
                    SetBasicArticleMetaData(item);
                    return;
                }

                // Meta tags provide extended data about the item, display as much as possible
                SetExtendedArticleMetaData(item, doc);
                item.Description = ApplyTemplateToDescription(item, ExtendedTemplate);
            }
            else
            {
                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(item);
                item.Description = ApplyTemplateToDescription(item, BasicTemplate);
            }

            log.Debug("{@item}", item);
        }

        private static string ApplyTemplateToDescription(RssFeedItem item, string template)
        {
            StringTemplate t = new StringTemplate(template);
            t.SetAttribute("item", item);
            return t.ToString();
        }

        private static void SetExtendedArticleMetaData(RssFeedItem item, HtmlDocument doc)
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
            var definition = ArticleDefinitions?.SingleOrDefault(p => p.SiteName == item.SiteName);

            if (definition == null)
            {
                // We don't have an article parser definition for this site, so just use the meta description
                item.ArticleText = $"<p>{item.MetaDescription}</p>";
            }
            else
            {
                // Add a cached instance of this parser if we don't already have one, using reflection
                if (!ArticleParserCache.ContainsKey(item.SiteName))
                {
                    Type type = Assembly.GetExecutingAssembly().GetType(definition.Parser);
                    ArticleParserCache.Add(item.SiteName, (IArticleParser)Activator.CreateInstance(type));
                }

                // Parse the article from the html
                var inst = ArticleParserCache[item.SiteName];
                item.ArticleText = inst.GetArticleBySelector(doc.Text, definition);
            }
        }

        private static void SetBasicArticleMetaData(RssFeedItem item)
        {
            item.HostName = new Uri(item.Url).GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
            item.SiteName = item.HostName;
            item.ArticleText = $"<p>Unable to crawl article content. Click the link below to view in your browser.</p>";
        }

        private static string ParseMetaTagAttributes(HtmlDocument doc, string property, string attribute)
        {
            // Retrieve the requested meta tag by property name
            var node = doc.DocumentNode.SelectSingleNode($"/html/head/meta[@property='{property}']");

            // Node can come back null if the meta tag is not present in the DOM
            // Attribute can come back null as well if not present on the meta tag
            string value = node?.Attributes[attribute]?.Value.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                log.Warning("Error reading attribute '{attribute}' from meta tag '{property}'", attribute, property);
            }

            return value;
        }

        private static void SaveUrlToDisk(RssFeedItem item, string workingFolder)
        {
            try
            {
                log.Information("Loading URL '{urlHash}':'{url}'", item.UrlHash, item.Url);

                // Use custom load method to account for compression headers
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(GetResponse(item.Url));
                doc.OptionFixNestedTags = true;

                // Construct unique file name
                string friendlyHostname = item.Url.Replace("://", "_").Replace(".", "_");
                friendlyHostname = friendlyHostname.Substring(0, friendlyHostname.IndexOf("/"));

                item.FileName = Path.Combine(workingFolder, $"{item.UrlHash}_{friendlyHostname}.html");

                // Delete the file if it already exists
                if (File.Exists(item.FileName))
                {
                    File.Delete(item.FileName);
                }

                log.Information("Saving file '{fileName}'", item.FileName);
                doc.Save(item.FileName);
            }
            catch (WebException ex)
            {
                log.Warning("Error loading url '{message}'", ex.Message);
            }
            catch (Exception ex)
            {
                log.Warning("Unexpected error loading url '{message}'", ex.Message);
            }
        }

        private static void SaveTextToDisk(string text, string filepath, bool deleteIfExists)
        {
            if (deleteIfExists && File.Exists(filepath))
            {
                File.Delete(filepath);
            }

            log.Information("Saving file '{filepath}'", filepath);

            // WriteAllText creates a file, writes the specified string to the file,
            // and then closes the file.    You do NOT need to call Flush() or Close().
            File.WriteAllText(filepath, text);
        }

        private static void BuildRssFileUsingTemplate(RssFeed sourceFeed, List<RssFeedItem> feedItems)
        {
            SyndicationFeed feed = new SyndicationFeed(sourceFeed.Title, sourceFeed.Description, new Uri(sourceFeed.Url), sourceFeed.Id.ToString(), DateTime.Now);

            List<SyndicationItem> syndicationItems = new List<SyndicationItem>();

            foreach (var item in feedItems)
            {
                syndicationItems.Add(new SyndicationItem(
                    item.Title.Replace("\u0008", ""),
                    item.Description.Replace("\u0008", ""),
                    new Uri(item.Url),
                    item.UrlHash,
                    DateTime.Now));
            }

            feed.Items = syndicationItems;

            string tempFile = Guid.NewGuid().ToString();    // CAUSES ACL PROBLEMS --> Path.GetTempFileName();
            log.Information("Writing out temp file '{fileName}'", tempFile);
            XmlWriter rssWriter = XmlWriter.Create(tempFile);

            Rss20FeedFormatter rssFormatter = new Rss20FeedFormatter(feed);
            rssFormatter.WriteTo(rssWriter);
            rssWriter.Close();

            // Delete the existing destination file
            // Added a 2 sec sleep to allow the delete process to finish
            int retries = 1;
            do
            {
                log.Information("Deleting existing destination file '{fileName}', try # {retries}", sourceFeed.OutputFile, retries);
                if (retries > 1)
                {
                    var r = new Random(DateTime.Now.Second);
                    Thread.Sleep(r.Next(2, 10));
                }

                try
                {
                    File.Delete(sourceFeed.OutputFile);
                }
                catch (IOException ex)
                {
                    log.Warning("Error deleting file on try # {retries}:{message}", retries, ex.Message);
                    retries++;
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Unexpected error deleting file on try # {retries}", retries);
                    throw;
                }

            } while (File.Exists(sourceFeed.OutputFile) && retries <= 5);

            log.Information("Rename temp file to destination file '{fileName}'", sourceFeed.OutputFile);
            File.Move(tempFile, sourceFeed.OutputFile);

            log.Information("Deleting temp file '{fileName}'", tempFile);
            File.Delete(tempFile);
        }
    }
}
