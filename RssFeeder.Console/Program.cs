using log4net;
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
using System.Xml;

namespace RssFeeder.Console
{
    /// <summary>
    /// A utility to retrieve RSS Feeds from remote servers
    /// </summary>
    class Program
    {
        private const string VERSION = "4.1";
        private const string DATEFORMAT = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

        private static ILog _log = null;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                // Display usage help and exit
                Usage();
                return;
            }

            // Start the logging
            log4net.Config.XmlConfigurator.Configure();
            _log = LogManager.GetLogger(typeof(Program));

            // Print banner to console
            _log.InfoFormat("RSSFeed {0}", VERSION);

            try
            {
                //using (IDocumentStore store = new DocumentStore
                //{
                //    Url = "http://localhost:8080/",
                //    DefaultDatabase = "RSSFeed"
                //})
                using (IDocumentStore store = new EmbeddableDocumentStore
                {
                    DataDirectory = "~/App_Data/Database",
                    DefaultDatabase = "RSSFeed"
                })
                {
                    store.Initialize();

                    List<Feed> feeds = GetFeeds(store);

                    foreach (var f in feeds)
                    {
                        _log.Info("Building feed links...");
                        BuildFeedLinks(store, f);
                        _log.Info("Building RSS files...");
                        BuildRssFile(store, f);
                        _log.Info("Removing stale links...");
                        RemoveFeedLinks(store, DateTime.Now.AddDays(-7));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
            }

#if DEBUG
            System.Console.WriteLine("\nPress <ENTER> to continue...");
            System.Console.ReadLine();
#endif
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

        private static void Usage()
        {
            System.Console.WriteLine("\nRSSFeed {0}", VERSION);
            System.Console.WriteLine("------------");
            System.Console.WriteLine("A utility to retrieve multiple RSS Feeds from remote servers");
            System.Console.WriteLine("Written by Matthew Noonan");
            System.Console.WriteLine("Sept 2003\n");
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
                var list = builder.Build(_log, feed);

                _log.Info("Adding links to database...");
                foreach (var item in list)
                {
                    AddLinkToDatabase(store, feed, item);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
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

                _log.InfoFormat("Visiting URL '{0}'", item.Url);
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

                _log.InfoFormat("ADDING: {0}|{1}", item.UrlHash, item.Title);
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
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 4;

            writer.WriteStartDocument();
            //			writer.WriteAttributeString("encoding", "UTF-8");

            writer.WriteComment("Generated by RSSFeed " + VERSION + " Copyright &copy " + DateTime.Now.Year + " Noonan Consulting Inc.");

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
            writer.WriteString("NCI RSSFeed " + VERSION);
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
