using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using Antlr4.StringTemplate;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommandLine;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using RssFeeder.Console.Database;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using RssFeeder.Models;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace RssFeeder.Console
{
    /// <summary>
    /// A utility to retrieve RSS Feeds from remote servers
    /// </summary>
    class Program
    {
        /// <summary>
        /// Name of the working area folder
        /// </summary>
        private const string WORKING_FOLDER = "working";

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

        private static ILogger log;

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
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(new RenderedCompactJsonFormatter(), "RssFeeder.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();
            Log.Logger = log;

            log.Information("START: Machine: {machineName} Assembly: {assembly}", Environment.MachineName, assemblyName.FullName);

            // Load configuration
            var configBuilder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddUserSecrets<Program>()
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = configBuilder.Build();
            var config = new CosmosDbConfig();
            configuration.GetSection("CosmosDB").Bind(config);
            log.Information("Loaded CosmosDB from config. Endpoint='{endpointUri}', authKey='{authKeyPartial}*****'", config.endpoint, config.authKey.Substring(0, 5));

            // Setup dependency injection
            var builder = new ContainerBuilder();

            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.Register(c => new CosmosDbRepository("rssfeeder", config.endpoint, config.authKey, Log.Logger)).As<IRepository>();
            builder.RegisterType<DrudgeReportFeedBuilder>().Named<IRssFeedBuilder>("drudge-report");
            builder.RegisterType<EagleSlantFeedBuilder>().Named<IRssFeedBuilder>("eagle-slant");

            var container = builder.Build();
            var serviceProvider = new AutofacServiceProvider(container);

            // set up TLS defaults
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Process the command line arguments
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => ProcessWithExitCode(opts, container))
                .WithNotParsed<Options>(errs => HandleParserError(errs, args))
                ;
        }

        static void HandleParserError(IEnumerable<CommandLine.Error> errors, string[] args)
        {
            // Return an error code
            log.Error("Invalid arguments: '{@args}'", args);
            Environment.Exit(255);
        }

        static void ProcessWithExitCode(Options opts, IContainer container)
        {
            // Zero return value means everything processed normally
            int returnCode = 0;

            try
            {
                List<Options> optionsList;
                var repository = container.Resolve<IRepository>();

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
                    optionsList = repository.GetDocuments<Options>("feeds", q => q.Title.Length > 0);
                }

                foreach (var option in optionsList)
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
                        Filters = option.Filters.ToList(),
                        CollectionName = option.CollectionName
                    };

                    using (LogContext.PushProperty("collectionName", f.CollectionName))
                    {
                        BuildFeedLinks(container, repository, f);
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
                    if (httpWebResponse.ContentEncoding?.ToLower().Contains("gzip") ?? false)
                    {
                        reader = new StreamReader(new GZipStream(httpWebResponse.GetResponseStream(), CompressionMode.Decompress));
                    }
                    else if (httpWebResponse.ContentEncoding?.ToLower().Contains("deflate") ?? false)
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

        private static void BuildFeedLinks(IContainer container, IRepository repository, RssFeed feed)
        {
            string html = WebTools.GetUrl(feed.Url);

            // Create the working folder for the collection if it doesn't exist
            string workingFolder = Path.Combine(AssemblyDirectory, feed.CollectionName);
            if (!Directory.Exists(workingFolder))
            {
                log.Information("Creating folder '{workingFolder}'", workingFolder);
                Directory.CreateDirectory(workingFolder);
            }

            // Save the feed html source for posterity
            string fileStem = Path.Combine(workingFolder, $"{DateTime.Now.ToUniversalTime():yyyyMMddhhmmss}_{feed.Url.Replace("://", "_").Replace(".", "_").Replace("/", "")}");
            SaveTextToDisk(html, fileStem + ".html", false);

            // Save thumbnail snapshot of the page
            SaveThumbnailToDisk(feed.Url, fileStem + ".png");

            // Parse the target links from the source to build the article crawl list
            var builder = container.ResolveNamed<IRssFeedBuilder>(feed.CollectionName);
            var list = builder.ParseRssFeedItems(feed, html)
                //.Take(10) FOR DEBUG PURPOSES
                ;

            // Load the collection of site parsers
            ArticleDefinitions = repository.GetDocuments<SiteArticleDefinition>("site-parsers", q => q.ArticleSelector.Length > 0);

            // Crawl any new articles and add them to the database
            log.Information("Adding new articles to the {collectionName} collection", feed.CollectionName);
            int count = 0;
            foreach (var item in list)
            {
                if (!repository.DocumentExists<RssFeedItem>(feed.CollectionName, q => q.UrlHash == item.UrlHash))
                {
                    count++;
                    SaveUrlToDisk(item, workingFolder);
                    ParseArticleMetaTags(item, feed);
                    repository.CreateDocument<RssFeedItem>(feed.CollectionName, item);
                }
            }
            log.Information("Added {count} new articles to the {collectionName} collection", count, feed.CollectionName);

            // Purge stale files from working folder
            short maximumAgeInDays = 7;
            PurgeStaleFiles(workingFolder, maximumAgeInDays);

            // Purge stale documents from the database collection
            list = repository.GetDocuments<RssFeedItem>(feed.CollectionName, q => q.DateAdded <= DateTime.Now.AddDays(-maximumAgeInDays));
            foreach (var item in list)
            {
                log.Information("Removing UrlHash '{urlHash}' from {collectionName}", item.UrlHash, feed.CollectionName);
                repository.DeleteDocument<RssFeedItem>(feed.CollectionName, item.Id, item.HostName);
            }
            log.Information("Removed {count} documents older than {maximumAgeInDays} days from {collectionName}", list.Count(), 7, feed.CollectionName);
        }

        private static void SaveThumbnailToDisk(string url, string filename)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("headless");//Comment if we want to see the window. 

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ChromeDriver driver = null;

            try
            {
                driver = new ChromeDriver(path, options);
                driver.Manage().Window.Size = new System.Drawing.Size(2000, 4000);
                driver.Navigate().GoToUrl(url);
                var screenshot = (driver as ITakesScreenshot).GetScreenshot();

                log.Information("Saving file '{filename}'", filename);
                screenshot.SaveAsFile(filename);
            }
            catch (Exception ex)
            {
                log.Error(ex, "ERROR: Unable to save webpage thumbnail");
            }
            finally
            {
                if (driver != null)
                {
                    driver.Close();
                    driver.Quit();
                }
            }
        }

        public static void PurgeStaleFiles(string folderPath, short maximumAgeInDays)
        {
            DateTime minimumDate = DateTime.Now.AddDays(-maximumAgeInDays);

            var files = Directory.EnumerateFiles(folderPath);
            int count = 0;

            foreach (var file in files)
            {
                if (DeleteFileIfOlderThan(file, minimumDate))
                {
                    count++;
                }
            }
            log.Information("Removed {count} files older than {maximumAgeInDays} days from {folderPath}", count, maximumAgeInDays, folderPath);
        }

        private static bool DeleteFileIfOlderThan(string path, DateTime date)
        {
            var file = new FileInfo(path);
            if (file.CreationTime < date)
            {
                log.Information("Removing {fileName}", file.FullName);
                file.Delete();
                return true;
            }

            return false;
        }

        private static void ParseArticleMetaTags(RssFeedItem item, RssFeed feed)
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
                item.Description = ApplyTemplateToDescription(item, feed, ExtendedTemplate);
            }
            else
            {
                // Article failed to download, display minimal basic meta data
                SetBasicArticleMetaData(item);
                item.Description = ApplyTemplateToDescription(item, feed, BasicTemplate);
            }

            log.Debug("{@item}", item);
        }

        private static string ApplyTemplateToDescription(RssFeedItem item, RssFeed feed, string template)
        {
            var t = new Template(template, '$', '$');
            t.Add("item", item);
            t.Add("feed", feed);

            return t.Render();
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
            catch (Exception ex)
            {
                log.Error(ex, "Unexpected error loading url '{message}'", ex.Message);
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
    }
}
