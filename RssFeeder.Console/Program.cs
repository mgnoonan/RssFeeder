using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using Antlr3.ST;
using CommandLine;
using HtmlAgilityPack;
using log4net;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using RssFeeder.Console.CustomBuilders;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;

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
        private const string ExtendedTemplate = @"<img src=""$item.ImageUrl$"" />
<h3>$item.Subtitle$</h3>
$item.ArticleText$
<p>
    <ul>
        <li><strong>Site Name:</strong> $item.SiteName$</li>
        <li><strong>URL:</strong> <a href=""$item.Url$"">$item.Url$</a></li>
        <li><strong>Hash:</strong> $item.UrlHash$</li>
    </ul>
</p>
";
        private const string BasicTemplate = @"<h3>$item.Title$</h3>
<p>
    <ul>
        <li><strong>Site Name:</strong> $item.SiteName$</li>
        <li><strong>URL:</strong> <a href=""$item.Url$"">$item.Url$</a></li>
        <li><strong>Hash:</strong> $item.UrlHash$</li>
    </ul>
</p>
";

        /// <summary>
        /// The Azure DocumentDB endpoint for running this GetStarted sample.
        /// </summary>
        private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUrl"];

        /// <summary>
        /// The primary key for the Azure DocumentDB account.
        /// </summary>
        private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"];

        /// <summary>
        /// Instance of the log4net logger
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The DocumentDB client instance.
        /// </summary>
        private static DocumentClient client;

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
            // DEBUG ONLY
            //TestParser();
            //return;

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

            if (!string.IsNullOrEmpty(definition.TestFilename))
            {
                html = File.ReadAllText(definition.TestFilename);
            }
            else
            {
                // set up TLS defaults
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                html = GetResponse(definition.TestUrl);
            }

            Type type = Assembly.GetExecutingAssembly().GetType(definition.Parser);
            parser = (IArticleParser)Activator.CreateInstance(type);

            // For console display, strip out the paragraph tags
            string text = parser.GetArticleBySelector(html, definition)
                .Replace("<p>", "")
                .Replace("</p>", "\n");

            System.Console.WriteLine(text);
        }

        static void HandleParserError(IEnumerable<Error> errors, string[] args)
        {
            // Return an error code
            log.Error(string.Format("Invalid arguments: '{0}'", string.Join(",", args)));
            Environment.Exit(255);
        }

        static void ProcessWithExitCode(Options opts)
        {
            // Grab the current assembly name
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            // Init the log4net through the config
            log4net.Config.XmlConfigurator.Configure();
            log.Info("--------------------------------");
            log.InfoFormat("Machine: {0}", Environment.MachineName);
            log.InfoFormat("Assembly: {0}", assemblyName.FullName);
            log.Info("--------------------------------");

            // set up TLS defaults
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            try
            {
                // A config file was specified so read in the options from there
                List<Options> optionsList;
                if (string.IsNullOrWhiteSpace(opts.Config))
                {
                    optionsList = new List<Options> { opts };
                }
                else
                {
                    // Get the directory of the current executable, all config 
                    // files should be in this path
                    string configFile = Path.Combine(AssemblyDirectory, opts.Config);
                    log.InfoFormat("Reading from config file: {0}", configFile);

                    using (StreamReader sr = new StreamReader(configFile))
                    {
                        // Read the options in JSON format
                        string json = sr.ReadToEnd();
                        log.InfoFormat("Options: {0}", json);

                        // Deserialize into our options class
                        optionsList = JsonConvert.DeserializeObject<List<Options>>(json);
                    }
                }

                // Create a new instance of the DocumentClient
                client = new DocumentClient(new Uri(EndpointUri), PrimaryKey);

                foreach (var option in optionsList)
                {
                    if (!string.IsNullOrWhiteSpace(option.TestDefinition))
                    {
                        using (StreamReader sr = new StreamReader(option.TestDefinition))
                        {
                            // Read the options in JSON format
                            string json = sr.ReadToEnd();
                            log.InfoFormat("Test article parser: {0}", json);

                            // Deserialize into our options class
                            var definition = JsonConvert.DeserializeObject<SiteArticleDefinition>(json);

                            TestArticleDefinition(definition);
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

                if (Environment.UserInteractive)
                {
                    System.Console.WriteLine("\nPress <Enter> to continue...");
                    System.Console.ReadLine();
                }

                // Zero return value means everything processed normally
                log.Info("Completed successfully");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                log.Error("Error during processing", ex);
                if (Environment.UserInteractive)
                {
                    System.Console.WriteLine("\nPress <Enter> to continue...");
                    System.Console.ReadLine();
                }
                Environment.Exit(250);
            }
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
            var list = builder.ParseRssFeedItems(log, feed)
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
                log.Info($"Creating folder '{workingFolder}'");
                Directory.CreateDirectory(workingFolder);
            }

#if DEBUG
            log.Debug($"Removing all files from '{workingFolder}'");
            foreach (var filename in Directory.EnumerateFiles(workingFolder))
            {
                File.Delete(filename);
            }
#endif

            // Add any links that don't already exist
            log.Info("Adding links to the database");
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
            log.Info($"Removing stale links from {databaseName}");
            list = GetStaleDocuments(databaseName, collectionName, 7);
            foreach (var item in list)
            {
                log.Info($"Removing {item.UrlHash}");
                DeleteDocument(databaseName, collectionName, item.Id);
            }

            // Purge stale files from working folder
            log.Info($"Removing stale files from {workingFolder}");
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
                log.Info($"Removing {file.FullName}");
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
            var result = client.CreateDocumentAsync(
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
            return client.CreateDocumentQuery<RssFeedItem>(
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
            var result = client.CreateDocumentQuery<RssFeedItem>(
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
            return client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .ToList();
        }

        private static void DeleteDocument(string databaseName, string collectionName, RssFeedItem item)
        {
            DeleteDocument(databaseName, collectionName, item.Id);
        }

        private static void DeleteDocument(string databaseName, string collectionName, string documentID)
        {
            var result = client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentID)).Result;

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
                log.Info($"Parsing meta tags from file '{item.FileName}'");

                var doc = new HtmlDocument();
                doc.Load(item.FileName);

                if (!doc.DocumentNode.HasChildNodes)
                {
                    log.Warn("No file content found, skipping.");
                    SetBasicArticleMetaData(item);
                    return;
                }

                // Meta tags provide extended data about the item, display as much as possible
                SetExtendedArticleMetaData(item, doc);
                ApplyTemplateToDescription(item, ExtendedTemplate);
            }
            else
            {
                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(item);
                ApplyTemplateToDescription(item, BasicTemplate);
            }

            log.Info(JsonConvert.SerializeObject(item, Formatting.Indented));
        }

        private static void ApplyTemplateToDescription(RssFeedItem item, string template)
        {
            StringTemplate t = new StringTemplate(template);
            t.SetAttribute("item", item);
            item.Description = t.ToString();
        }

        private static void SetExtendedArticleMetaData(RssFeedItem item, HtmlDocument doc)
        {
            // Extract the meta data from the Open Graph tags helpfully provided with almost every article
            item.Subtitle = ParseMetaTagAttributes(doc, "og:title", "content");
            item.ImageUrl = ParseMetaTagAttributes(doc, "og:image", "content");
            item.MetaDescription = ParseMetaTagAttributes(doc, "og:description", "content");
            item.SiteName = ParseMetaTagAttributes(doc, "og:site_name", "content").ToLower();
            if (string.IsNullOrWhiteSpace(item.SiteName))
            {
                item.SiteName = string.IsNullOrWhiteSpace(item.Url) ? "" : new Uri(item.Url).GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
            }

            // Check if we have a site parser defined for the site name
            var definition = ArticleDefinitions.SingleOrDefault(p => p.SiteName == item.SiteName);

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
            item.SiteName = string.IsNullOrWhiteSpace(item.Url) ? "" : new Uri(item.Url).GetComponents(UriComponents.Host, UriFormat.Unescaped).ToLower();
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
                log.Warn($"Error reading attribute '{attribute}' from meta tag '{property}'");
            }

            return value;
        }

        private static void SaveUrlToDisk(RssFeedItem item, string workingFolder)
        {
            try
            {
                log.Info($"Loading URL '{item.UrlHash}':'{item.Url}'");

                // Use custom load method to account for compression headers
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(GetResponse(item.Url));
                doc.OptionFixNestedTags = true;

                // Construct unique file name
                item.FileName = Path.Combine(workingFolder, $"{item.UrlHash}.html");
                if (File.Exists(item.FileName))
                {
                    File.Delete(item.FileName);
                }

                log.Info($"Saving file '{item.FileName}'");
                doc.Save(item.FileName);
            }
            catch (WebException ex)
            {
                log.Warn($"Error loading url '{ex.Message}'");
            }
            catch (Exception ex)
            {
                log.Warn($"Unexpected error loading url '{ex.Message}'");
            }
        }

        private static void BuildRssFileUsingTemplate(RssFeed feed, List<RssFeedItem> items)
        {
            var group = new StringTemplateGroup("myGroup", Path.Combine(AssemblyDirectory, "Templates"));
            var t = group.GetInstanceOf("DrudgeFeed");

            t.SetAttribute("feed", feed);
            t.SetAttribute("items", items);
            t.SetAttribute("copyrightYear", DateTime.Now.Year);
            t.SetAttribute("feedGenerator", "NCI RSSFeeder " + Assembly.GetExecutingAssembly().GetName().Version);
            t.SetAttribute("feedPubDate", DateTime.Now.Date.ToUniversalTime().ToString(DATEFORMAT));
            t.SetAttribute("feedLastBuildDate", DateTime.Now.ToUniversalTime().ToString(DATEFORMAT));

            string tempFile = Guid.NewGuid().ToString();    // CAUSES ACL PROBLEMS --> Path.GetTempFileName();

            log.Info($"Writing out temp file '{tempFile}'");
            using (var writer = new StreamWriter(tempFile, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine(t.ToString());
            }

            // Delete the existing destination file
            // Added a 2 sec sleep to allow the delete process to finish
            int retries = 1;
            do
            {
                log.Info($"Deleting existing destination file '{feed.OutputFile}', try # {retries}");
                if (retries > 1)
                {
                    Thread.Sleep(2000);
                }

                File.Delete(feed.OutputFile);
                retries++;

            } while (File.Exists(feed.OutputFile) && retries <= 5);

            log.Info($"Rename temp file to destination file '{feed.OutputFile}'");
            File.Move(tempFile, feed.OutputFile);

            log.Info($"Deleting temp file '{tempFile}'");
            File.Delete(tempFile);
        }
    }
}
