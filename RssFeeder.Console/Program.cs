using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using HtmlAgilityPack;
using log4net;
using Newtonsoft.Json;
using Raven.Client;
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
        /// Instance of the log4net logger
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // Grab the current assembly name
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            // Init the log4net through the config
            log4net.Config.XmlConfigurator.Configure();
            log.Info("--------------------------------");
            log.InfoFormat("Machine: {0}", Environment.MachineName);
            log.InfoFormat("Assembly: {0}", assemblyName.FullName);
            log.Info("--------------------------------");

            // Process the command line arguments
            var commandLineOptions = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, commandLineOptions))
            {
                // Return an error code
                log.Error(string.Format("Invalid arguments: '{0}'", string.Join(",", args)));
                return 255;
            }

            try
            {
                // A config file was specified so read in the options from there
                List<Options> optionsList;
                if (string.IsNullOrWhiteSpace(commandLineOptions.Config))
                {
                    optionsList = new List<Options> { commandLineOptions };
                }
                else
                {
                    // Get the directory of the current executable, all config 
                    // files should be in this path
                    string configFile = AssemblyDirectory + commandLineOptions.Config;
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
                        Filters = option.Filters
                    };

                    var items = BuildFeedLinks(f);
                    BuildRssFile(f, items);
                }

#if DEBUG
                System.Console.WriteLine("\nPress <Enter> to continue...");
                System.Console.ReadLine();
#endif

                // Zero return value means everything processed normally
                log.Info("Completed successfully");
                return 0;
            }
            catch (Exception ex)
            {
                log.Error("Error during processing", ex);
#if DEBUG
                System.Console.WriteLine("\nPress <Enter> to continue...");
                System.Console.ReadLine();
#endif
                return 250;
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

        private static List<Feed> GetFeeds(IDocumentStore store)
        {
            using (IDocumentSession documentSession = store.OpenSession())
            {
                var feeds = documentSession.Query<Feed>().ToList();

                if (!feeds.Any())
                {
                    Feed newFeed = new Feed
                    {
                        Title = "The Drudge Report",
                        Url = "http://www.drudgereport.com",
                        Description = "The Drudge Report",
                        Filename = @"drudge.xml",
                        Language = "en-US",
                        CustomParser = "RSSFeed.CustomBuilders.DrudgeReportFeedBuilder",
                        Filters = new List<string>
                        {
                            "cd0802700cef27a775ab057a2ef54aea",
                            "cf69b6dd74d44010fdd0eff6d778e70f",
                            "3277518fd12233369747e13dc415de14",
                            "3c0e6e6b7eb1f7563eeacc7a9165407a",
                            "aac71d089541a02789fe80fc806dbabf",
                            "0cdb9a4ef88173081464cbb9e8000e86"
                        }
                    };

                    feeds.Add(newFeed);
                    documentSession.Store(newFeed);
                    documentSession.SaveChanges();
                }

                return feeds;
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
                .Take(10)
                ;

            log.Info("Adding links to database");
            foreach (var item in list)
            {
                //AddLinkToDatabase(store, feed, item);

                SaveUrlToDisk(item);
                ParseMetaTags(item);
            }

            return list.ToList();
        }

        private static void ParseMetaTags(FeedItem item)
        {
            if (File.Exists(item.FileName))
            {
                log.Info($"Parsing meta tags from file '{item.FileName}'");

                var doc = new HtmlDocument();
                doc.Load(item.FileName);

                if (!doc.DocumentNode.HasChildNodes)
                {
                    log.Warn("No file content found, skipping.");
                    return;
                }

                var title = ParseMetaTagAttributes(doc, "og:title", "content");
                item.Url = ParseMetaTagAttributes(doc, "og:url", "content");
                item.ImageUrl = ParseMetaTagAttributes(doc, "og:image", "content");
                item.SiteName = ParseMetaTagAttributes(doc, "og:site_name", "content");
                item.Description = $"<img src=\"{item.ImageUrl}\" />\r\n<h1>{title}<h1>\r\n<p><a href=\"{item.Url}\"></a></p>\r\n<p>{ParseMetaTagAttributes(doc, "og:description", "content")}</p>";
            }

            log.Info(JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented));
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

        private static void RemoveFeedLinks(IDocumentStore store, DateTime targetDate)
        {
            using (IDocumentSession documentSession = store.OpenSession())
            {
                var query = documentSession.Query<FeedItem>().Where(i => i.DateAdded < targetDate);

                foreach (var item in query)
                {
                    documentSession.Delete<FeedItem>(item);
                }

                documentSession.SaveChanges();
            }
        }

        private static void AddLinkToDatabase(IDocumentStore store, Feed feed, FeedItem item)
        {
            using (IDocumentSession documentSession = store.OpenSession())
            {
                if (documentSession.Query<FeedItem>().Any(i => i.UrlHash == item.UrlHash))
                {
                    return;
                }

                string description = string.Empty;

                log.InfoFormat("Visiting URL '{0}'", item.Url);
                description = Utility.Utility.GetParagraphTagsFromHtml(item.Url);
                item.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

                log.InfoFormat("ADDING: {0}|{1}", item.UrlHash, item.Title);
                documentSession.Store(item);
                documentSession.SaveChanges();

                return;
            }
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

                string workingFolder = $"{AssemblyDirectory}\\{WORKING_FOLDER}";
                if (!Directory.Exists(workingFolder))
                {
                    log.Info($"Creating folder '{workingFolder}'");
                    Directory.CreateDirectory(workingFolder);
                }

                // Construct unique file name
                item.FileName = $"{workingFolder}\\{item.UrlHash}.html";
                if (File.Exists(item.FileName))
                {
                    File.Delete(item.FileName);
                }

                log.Info($"Saving file '{item.FileName}'");
                doc.Save(item.FileName);
            }
            catch (System.Net.WebException ex)
            {
                log.Warn($"Error loading url '{ex.Message}'");
            }
            catch (Exception ex)
            {
                log.Warn($"Unexpected error loading url '{ex.Message}'");
            }
        }

        private static void BuildRssFile(Feed feed, List<FeedItem> items)
        {
            // Build the initial RSS 2.0 document
            string url = feed.Url;
            string channelTitle = feed.Title;
            string relativeRoot = feed.Url;
            string tempFile = Guid.NewGuid().ToString();    // CAUSES ACL PROBLEMS --> Path.GetTempFileName();
            var targetDate = DateTime.Now.AddDays(-1);

            using (XmlTextWriter writer = new XmlTextWriter(tempFile, System.Text.Encoding.UTF8))
            {
                WriteRssHeader(writer, feed, null);
                foreach (var item in items)
                {
                    WriteRssItem(writer, item);
                }
                WriteRssFooter(writer);
                writer.Flush();
                writer.Close();
            }

            // Rename the temp file
            File.Delete(feed.Filename);
            File.Move(tempFile, feed.Filename);
            File.Delete(tempFile);
        }

        private static void WriteRssHeader(XmlTextWriter writer, Feed feed, FeedItem imageItem)
        {
            writer.Formatting = System.Xml.Formatting.Indented;
            writer.Indentation = 4;

            writer.WriteStartDocument();
            //			writer.WriteAttributeString("encoding", "UTF-8");

            writer.WriteComment("Generated by RSSFeed " + Assembly.GetExecutingAssembly().GetName().Version + " Copyright &copy " + DateTime.Now.Year + " Noonan Consulting Inc.");

            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");

            writer.WriteStartElement("channel");

            writer.WriteStartElement("title");
            writer.WriteString(feed.Title);
            writer.WriteEndElement();

            writer.WriteStartElement("link");
            writer.WriteString(feed.Url);
            writer.WriteEndElement();

            writer.WriteStartElement("description");
            writer.WriteString(feed.Title);
            writer.WriteEndElement();

            writer.WriteStartElement("generator");
            writer.WriteString("NCI RSSFeed " + Assembly.GetExecutingAssembly().GetName().Version);
            writer.WriteEndElement();

            if (imageItem != null)
            {
                writer.WriteStartElement("image");
                writer.WriteStartElement("url");
                writer.WriteString(imageItem.ImageUrl);
                writer.WriteEndElement();
                writer.WriteStartElement("title");
                writer.WriteString(feed.Title);
                writer.WriteEndElement();
                writer.WriteStartElement("link");
                writer.WriteString(feed.Url);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteStartElement("pubDate");
            writer.WriteString(DateTime.Now.Date.ToUniversalTime().ToString(DATEFORMAT));
            writer.WriteEndElement();

            writer.WriteStartElement("lastBuildDate");
            writer.WriteString(DateTime.Now.ToUniversalTime().ToString(DATEFORMAT));
            writer.WriteEndElement();
        }

        private static void WriteRssFooter(XmlTextWriter writer)
        {
            writer.WriteEndElement();	// Channel
            writer.WriteEndElement();	// Rss
        }

        private static void WriteRssItem(XmlTextWriter writer, FeedItem item)
        {
            writer.WriteStartElement("item");

            writer.WriteStartElement("title");
            writer.WriteString(item.Title.Trim());
            writer.WriteEndElement();

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                writer.WriteStartElement("description");
                writer.WriteString(item.Description.Trim());
                writer.WriteEndElement();
            }

            writer.WriteStartElement("link");
            writer.WriteString(item.Url.Trim());
            writer.WriteEndElement();

            writer.WriteStartElement("guid");
            writer.WriteString(item.UrlHash);
            writer.WriteEndElement();

            writer.WriteStartElement("pubDate");
            writer.WriteString(item.DateAdded.ToUniversalTime().ToString(DATEFORMAT));
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
