using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using RssFeeder.Console.HttpClients;
using Serilog;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// Summary description for WebTools.
    /// </summary>
    public class WebUtils : IWebUtils
    {
        private readonly IHttpClient _crawler;

        public WebUtils(IHttpClient crawler)
        {
            _crawler = crawler;
        }

        public (string, Uri) SaveUrlToDisk(string url, string urlHash, string filename, bool removeScriptElements = true)
        {
            if (!filename.EndsWith(".html"))
                return (SaveImageToDisk(url, urlHash, filename), new Uri(url));

            try
            {
                Log.Information("Loading URL '{urlHash}':'{url}'", urlHash, url);
                (string content, Uri trueUri) = _crawler.GetString(url);

                // Use custom load method to account for compression headers
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(content);
                doc.OptionFixNestedTags = true;

                // List of html tags we really don't care to save
                var excludeHtmlTags = new List<string> { "style", "link", "svg" };
                if (removeScriptElements)
                {
                    excludeHtmlTags.Add("script");
                }

                doc.DocumentNode
                    .Descendants()
                    .Where(n => excludeHtmlTags.Contains(n.Name))
                    .ToList()
                    .ForEach(n => n.Remove());

                // Delete the file if it already exists
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                Log.Information("Saving text file '{fileName}'", filename);
                doc.Save(filename);

                return (filename, trueUri);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SaveUrlToDisk: '{urlHash}':'{url}' - Unexpected error '{message}'", urlHash, url, ex.Message);
            }

            return (string.Empty, new Uri(url));
        }

        private string SaveImageToDisk(string url, string urlHash, string filename)
        {
            try
            {
                Log.Information("Loading image URL '{urlHash}':'{url}'", urlHash, url);
                var fileBytes = _crawler.DownloadData(url);

                // Delete the file if it already exists
                if (File.Exists(filename))
                {
                    Log.Information("Delete existing image file '{fileName}'", filename);
                    File.Delete(filename);
                }

                // if the remote file was found, download it
                Log.Information("Saving image file '{fileName}' {bytes} bytes", filename, fileBytes.Length);
                File.WriteAllBytes(filename, fileBytes);

                return filename;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SaveUrlToDisk: '{urlHash}':'{url}' - Unexpected error '{message}'", urlHash, url, ex.Message);
            }

            return string.Empty;
        }

        public (string, Uri) WebDriverUrlToDisk(string url, string urlHash, string filename)
        {
            Log.Information("Loading Selenium URL '{urlHash}':'{url}'", urlHash, url);

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("headless");//Comment if we want to see the window. 

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ChromeDriver driver = null;

            try
            {
                driver = new ChromeDriver(path, options);
                driver.Navigate().GoToUrl(url);

                // Delete the file if it already exists
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                Log.Information("Saving text file '{fileName}'", filename);
                File.WriteAllText(filename, driver.PageSource);

                return (filename, new Uri(driver.Url));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "WebDriverUrlToDisk: '{urlHash}':'{url}' - Unexpected error '{message}'", urlHash, url, ex.Message);
            }
            finally
            {
                if (driver != null)
                {
                    driver.Close();
                    driver.Quit();
                }
            }

            return (string.Empty, new Uri(url));
        }

        public void SaveThumbnailToDisk(string url, string filename)
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
                Thread.Sleep(5000);
                var screenshot = (driver as ITakesScreenshot).GetScreenshot();

                Log.Information("Saving thumbnail file '{filename}'", filename);
                screenshot.SaveAsFile(filename);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ERROR: Unable to save webpage thumbnail");
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

        /// <summary>
        /// Attempt to repair typos in the URL, usually in the protocol section
        /// </summary>
        /// <param name="pathAndQuery">The path and query of the URL which may be a relative URL or a botched URL with some kind of typo in it</param>
        /// <param name="defaultBaseUrl">The default base URL to use if a relative URL is detected</param>
        /// <returns>
        /// The sanitized and repaired URL, although not all repairs will be successful
        /// </returns>
        public string RepairUrl(string pathAndQuery, string defaultBaseUrl)
        {
            Log.Information("Attempting to repair link '{url}'", pathAndQuery);
            StringBuilder sb = new StringBuilder();

            if (pathAndQuery.StartsWith("//"))
            {
                // They specified a protocol-independent URL, force it to https
                sb.AppendFormat("https:{0}", pathAndQuery);
            }
            else
            {
                if (pathAndQuery.Contains("//"))
                {
                    // They did try to specify a base url, but clearly messed it up because it doesn't start with 'http'
                    // Get the starting position of the double-slash, we'll use everything after that
                    int pos = pathAndQuery.IndexOf("//") + 2;

                    // At this point in web history, we should be able to just use SSL/TLS for the protocol
                    // However, this may fail if the destination site doesn't support SSL/TLS so we are taking a risk
                    sb.AppendFormat("https://{0}", pathAndQuery.Substring(pos));
                }
                else
                {
                    // Relative path specified, or they goofed the url beyond repair so this is the best we can do
                    // Start with the defaultBaseUrl and add a trailing forward slash
                    sb.AppendFormat("{0}{1}", defaultBaseUrl.Trim(), defaultBaseUrl.EndsWith("/") ? "" : "/");

                    // Add the starting forward slash if there isn't one
                    if (!pathAndQuery.StartsWith("/"))
                    {
                        sb.Append("/");
                    }

                    sb.Append(pathAndQuery);
                }
            }

            Log.Information("Repaired link '{url}'", sb.ToString());
            return sb.ToString();
        }

        public (string, Uri) DownloadString(string url)
        {
            return _crawler.GetString(url);
        }

        public string GetContentType(string url)
        {
            return _crawler.GetContentType(url);
        }
    }
}
