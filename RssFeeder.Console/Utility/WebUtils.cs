using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Polly;
using Serilog;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// Summary description for WebTools.
    /// </summary>
    public class WebUtils : IWebUtils
    {
        private const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4170.0 Safari/537.36 Edg/85.0.552.1";
        private static HttpClient _client;

        public WebUtils()
        {
            if (_client == null)
            {
                _client = new HttpClient(new SocketsHttpHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        AllowRenegotiation = true,
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12
                    },
                    UseCookies = true
                });

                _client.DefaultRequestHeaders.Add("user-agent", USERAGENT);
            }
        }

        public string DownloadStringWithCompression(string url)
        {
            return DownloadStringWithCompressionAsync(url).GetAwaiter().GetResult();
        }

        public async Task<string> DownloadStringWithCompressionAsync(string url)
        {
            string result = await Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, timeSpan, retryCount, context) =>
                {
                    Log.Warning("Error downloading '{url}' retry={retryCount} waiting {ts} seconds. Stack trace={stackTrace}", url, retryCount, timeSpan.TotalSeconds, ex.StackTrace);
                })
            .ExecuteAsync(() => _client.GetStringAsync(url));

            return result;
        }

        public string SaveUrlToDisk(string url, string urlHash, string filename, bool removeScriptElements = true)
        {
            try
            {
                Log.Logger.Information("Loading URL '{urlHash}':'{url}'", urlHash, url);

                // List of html tags we really don't care to save
                var excludeHtmlTags = new List<string> { "script", "style", "link", "svg" };

                // Use custom load method to account for compression headers
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(DownloadStringWithCompression(url));
                doc.OptionFixNestedTags = true;

                if (removeScriptElements)
                {
                    doc.DocumentNode
                        .Descendants()
                        .Where(n => excludeHtmlTags.Contains(n.Name))
                        .ToList()
                        .ForEach(n => n.Remove());
                }

                // Delete the file if it already exists
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }

                Log.Logger.Information("Saving text file '{fileName}'", filename);
                doc.Save(filename);

                return filename;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "SaveUrlToDisk: '{urlHash}':'{url}' - Unexpected error '{message}'", urlHash, url, ex.Message);
            }

            return string.Empty;
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
                var screenshot = (driver as ITakesScreenshot).GetScreenshot();

                Log.Logger.Information("Saving thumbnail file '{filename}'", filename);
                screenshot.SaveAsFile(filename);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "ERROR: Unable to save webpage thumbnail");
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
        /// <param name="defaultBaseUrl">The default base URL to use if a relative URL is detected</param>
        /// <param name="pathAndQuery">The path and query of the URL which may be a relative URL or a botched URL with some kind of typo in it</param>
        /// <returns>
        /// The sanitized and repaired URL, although not all repairs will be successful
        /// </returns>
        public string RepairUrl(string pathAndQuery)
        {
            StringBuilder sb = new StringBuilder();

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
                //sb.AppendFormat("{0}{1}", defaultBaseUrl.Trim(), defaultBaseUrl.EndsWith("/") ? "" : "/");

                // Add the starting forward slash if there isn't one
                if (!pathAndQuery.StartsWith("/"))
                {
                    sb.Append("/");
                }

                sb.Append(pathAndQuery);
            }

            return sb.ToString();
        }
    }
}
