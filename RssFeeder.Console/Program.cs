using log4net;
using Newtonsoft.Json;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using RssFeeder.Console.CustomBuilders;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace RssFeeder.Console
{
    /// <summary>
    /// A utility to retrieve RSS Feeds from remote servers
    /// </summary>
    class Program
    {
        private const string DATEFORMAT = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

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
            AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();

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

                    using (IDocumentStore store = new EmbeddableDocumentStore
                    {
                        DataDirectory = "~/App_Data/Database",
                        DefaultDatabase = "RSSFeed"
                    })
                    {
                        store.Initialize();

                        //List<Feed> feeds = GetFeeds(store);

                        //foreach (var f in feeds)
                        //{
                        log.Info("Building feed links...");
                        BuildFeedLinks(store, f);
                        log.Info("Building RSS files...");
                        BuildRssFile(store, f);
                        log.Info("Removing stale links...");
                        RemoveFeedLinks(store, DateTime.Now.AddDays(-7));
                        //}
                    }
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

        private static void BuildFeedLinks(IDocumentStore store, Feed feed)
        {
            string builderName = feed.CustomParser;
            if (string.IsNullOrWhiteSpace(builderName))
                return;

            try
            {
                Type type = System.Reflection.Assembly.GetExecutingAssembly().GetType(builderName);
                ICustomFeedBuilder builder = (ICustomFeedBuilder)Activator.CreateInstance(type);
                var list = builder.Build(log, feed);

                log.Info("Adding links to database...");
                foreach (var item in list)
                {
                    AddLinkToDatabase(store, feed, item);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }
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
                string imageUrl = string.Empty;

                log.InfoFormat("Visiting URL '{0}'", item.Url);
                description = Utility.Utility.GetParagraphTagsFromHtml(item.Url);
                item.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
                //ISiteParser parser;
                //if (item.Url.Contains("breitbart.com"))
                //{
                //    parser = new BreitbartParser(_log);
                //}
                //else if (item.Url.Contains("telegraph.co.uk"))
                //{
                //    parser = new TelegraphUkParser(_log);
                //}
                //else
                //{
                //parser = new GenericParser(_log);
                //}

                //parser.Load(item);
                //parser.Parse();

                log.InfoFormat("ADDING: {0}|{1}", item.UrlHash, item.Title);
                documentSession.Store(item);
                documentSession.SaveChanges();

                return;
            }
        }

        private static void BuildRssFile(IDocumentStore store, Feed feed)
        {
            // Build the initial RSS 2.0 document
            string url = feed.Url;
            string channelTitle = feed.Title;
            string relativeRoot = feed.Url;
            string tempFile = Guid.NewGuid().ToString();    // CAUSES ACL PROBLEMS --> Path.GetTempFileName();
            var targetDate = DateTime.Now.AddDays(-1);

            FeedItem imageItem = null;
            List<FeedItem> items = null;
            using (IDocumentSession documentSession = store.OpenSession())
            {
                items = documentSession.Query<FeedItem>()
                    .Where(i => i.FeedId == feed.Id && i.DateAdded > targetDate)
                    .OrderByDescending(i => i.DateAdded)
                    .ToList();
            }

            using (XmlTextWriter writer = new XmlTextWriter(tempFile, System.Text.Encoding.UTF8))
            {

                WriteRssHeader(writer, feed, imageItem);
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

            writer.WriteComment("Generated by RSSFeed " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " Copyright &copy " + DateTime.Now.Year + " Noonan Consulting Inc.");

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
            writer.WriteString("NCI RSSFeed " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
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
