using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Antlr3.ST;
using CommandLine;
using HtmlAgilityPack;
using log4net;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using RssFeeder.Console.CustomBuilders;
using RssFeeder.Console.Models;

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
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Process the command line arguments
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => ProcessWithExitCode(opts))
                .WithNotParsed<Options>(errs => HandleParserError(errs, args))
                ;
        }

        static void HandleParserError(IEnumerable<CommandLine.Error> errors, string[] args)
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
                    // Transform the option to the old style feed
                    var f = new Feed
                    {
                        Title = option.Title,
                        Description = option.Description,
                        Filename = option.Filename,
                        Language = option.Language,
                        Url = option.Url,
                        CustomParser = option.CustomParser,
                        Filters = option.Filters.ToList()
                    };

                    var items = BuildFeedLinks(f);
                    BuildRssFileUsingTemplate(f, items);
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

        private static List<FeedItem> BuildFeedLinks(Feed feed)
        {
            string builderName = feed.CustomParser;
            if (string.IsNullOrWhiteSpace(builderName))
                return new List<FeedItem>();

            Type type = Assembly.GetExecutingAssembly().GetType(builderName);
            ICustomFeedBuilder builder = (ICustomFeedBuilder)Activator.CreateInstance(type);
            var list = builder.ParseFeedItems(log, feed)
                //.Take(10) FOR DEBUG PURPOSES
                ;

            string databaseName = "rssfeeder";
            string collectionName = "drudge-report";

            // Add any links that don't already exist
            log.Info("Adding links to the database");
            foreach (var item in list)
            {
                if (!DocumentExists(databaseName, collectionName, item))
                {
                    SaveUrlToDisk(item);
                    ParseMetaTags(item);
                    CreateDocument(databaseName, collectionName, item);
                }
            }

            // Remove any stale documents
            log.Info("Removing stale links from the database");
            list = GetStaleDocuments(databaseName, collectionName, DateTime.Now.AddDays(-7));
            foreach (var item in list)
            {
                DeleteDocument(databaseName, collectionName, item.id);
            }

            // Return whatever documents are left in the database
            return GetAllDocuments(databaseName, collectionName);
        }

        /// <summary>
        /// Create the Family document in the collection if another by the same partition key/item key doesn't already exist.
        /// </summary>
        /// <param name="databaseName">The name/ID of the database.</param>
        /// <param name="collectionName">The name/ID of the collection.</param>
        /// <param name="item"></param>
        private static void CreateDocument(string databaseName, string collectionName, FeedItem item)
        {
            var result = client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), item)
                .Result;

            if (result.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception($"Unable to create document for '{item.UrlHash}'");
            }
        }

        private static List<FeedItem> GetStaleDocuments(string databaseName, string collectionName, DateTime targetDate)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            return client.CreateDocumentQuery<FeedItem>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .Where(f => f.DateAdded <= targetDate)
                .ToList();
        }

        private static bool DocumentExists(string databaseName, string collectionName, FeedItem item)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            var result = client.CreateDocumentQuery<FeedItem>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .Where(f => f.UrlHash == item.UrlHash);

            return result.Count() > 0;
        }

        private static List<FeedItem> GetAllDocuments(string databaseName, string collectionName)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Run a simple query via LINQ. DocumentDB indexes all properties, so queries 
            // can be completed efficiently and with low latency
            return client.CreateDocumentQuery<FeedItem>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                .OrderByDescending(i => i.DateAdded)
                .ToList();
        }
        private static void DeleteDocument(string databaseName, string collectionName, FeedItem item)
        {
            DeleteDocument(databaseName, collectionName, item.id);
        }

        private static void DeleteDocument(string databaseName, string collectionName, string documentID)
        {
            var result = client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, documentID)).Result;

            if (result.StatusCode != HttpStatusCode.NoContent)
            {
                throw new Exception($"Unable to delete document for '{documentID}'");
            }
        }

        private static void ParseMetaTags(FeedItem item)
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
                    SetBasicItemInfo(item);
                    return;
                }

                // Meta tags provide extended data about the item, display as much as possible
                SetExtendedItemInfo(item, doc);
            }
            else
            {
                // Article failed to download, display minimal basic meta data
                SetBasicItemInfo(item);
            }

            log.Info(JsonConvert.SerializeObject(item, Formatting.Indented));
        }

        private static void SetExtendedItemInfo(FeedItem item, HtmlDocument doc)
        {
            item.Subtitle = ParseMetaTagAttributes(doc, "og:title", "content");
            item.Url = ParseMetaTagAttributes(doc, "og:url", "content");
            item.ImageUrl = ParseMetaTagAttributes(doc, "og:image", "content");
            item.MetaDescription = ParseMetaTagAttributes(doc, "og:description", "content");
            item.SiteName = ParseMetaTagAttributes(doc, "og:site_name", "content");
            if (string.IsNullOrWhiteSpace(item.SiteName))
            {
                item.SiteName = new Uri(item.Url).GetComponents(UriComponents.Host, UriFormat.Unescaped);
            }

            StringTemplate t = new StringTemplate(@"<img src=""$item.ImageUrl$"" />
<h2>$item.Subtitle$</h2>
<p>
    $item.MetaDescription$
</p>
<p>
    <ul>
        <li><strong>Site Name:</strong> $item.SiteName$</li>
        <li><strong>Hash:</strong> $item.UrlHash$</li>
        <li><strong>URL:</strong> <a href=""$item.Url$"">$item.Url$</a></li>
    </ul>
</p>
");
            t.SetAttribute("item", item);
            item.Description = t.ToString();
        }

        private static void SetBasicItemInfo(FeedItem item)
        {
            item.SiteName = new Uri(item.Url).GetComponents(UriComponents.Host, UriFormat.Unescaped);

            StringTemplate t = new StringTemplate(@"<h2>$item.Title$</h2>
<p>
    <ul>
        <li><strong>Site Name:</strong> $item.SiteName$</li>
        <li><strong>Hash:</strong> $item.UrlHash$</li>
        <li><strong>URL:</strong> <a href=""$item.Url$"">$item.Url$</a></li>
    </ul>
</p>
");
            t.SetAttribute("item", item);
            item.Description = t.ToString();
        }

        private static string ParseMetaTagAttributes(HtmlDocument doc, string property, string attribute)
        {
            // Retrieve the requested meta tag by property name
            var node = doc.DocumentNode.SelectSingleNode($"/html/head/meta[@property='{property}']");

            // Node can come back null if the meta tag is not present in the DOM
            string value = node?.Attributes[attribute].Value.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                log.Warn($"Error reading attribute '{attribute}' from meta tag '{property}'");
            }

            return value;
        }

        private static void SaveUrlToDisk(FeedItem item)
        {
            try
            {
                log.Info($"Loading URL '{item.Url}'");

                // Load the initial HTML from the URL
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument doc = hw.Load(item.Url);
                doc.OptionFixNestedTags = true;

                string workingFolder = Path.Combine(AssemblyDirectory, WORKING_FOLDER);
                if (!Directory.Exists(workingFolder))
                {
                    log.Info($"Creating folder '{workingFolder}'");
                    Directory.CreateDirectory(workingFolder);
                }

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

        private static void BuildRssFileUsingTemplate(Feed feed, List<FeedItem> items)
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

            using (var writer = new StreamWriter(tempFile, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine(t.ToString());
            }

            // Rename the temp file
            File.Delete(feed.Filename);
            File.Move(tempFile, feed.Filename);
            File.Delete(tempFile);
        }
    }
}
