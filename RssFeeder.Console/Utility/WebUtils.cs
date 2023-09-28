using System.Reflection;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using RssFeeder.Console.HttpClients;

namespace RssFeeder.Console.Utility;

/// <summary>
/// Summary description for WebTools.
/// </summary>
public class WebUtils : IWebUtils
{
    private readonly IHttpClient _crawler;
    private readonly ILogger _log;

    public WebUtils(IHttpClient crawler, ILogger log)
    {
        _crawler = crawler;
        _log = log;
    }

    public (HttpStatusCode status, string content, Uri trueUri, string contentType) ClientGetString(string url)
    {
        bool retry;

        try
        {
            _log.Debug("ClientGetString: Loading URL '{url}'", url);
            return _crawler.GetString(url);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "ClientGetString: Unexpected error '{message}'", ex.Message);
            retry = ex.Message.Contains("Moved") || ex.Message.Contains("Request timed out");
        }

        // Detect a retry situation where we will eventually crawl using the web driver
        return (retry ? HttpStatusCode.Found : HttpStatusCode.Forbidden, null, new Uri(url), null);
    }

    public byte[] ClientGetBytes(string url)
    {
        _log.Debug("ClientGetBytes: Loading URL '{url}'", url);
        return _crawler.DownloadData(url);
    }

    public (HttpStatusCode status, string content, Uri trueUri, string contentType) DriverGetString(string url)
    {
        _log.Information("DriverGetString: Loading URL '{url}'", url);

        var options = new EdgeOptions();
        options.AddArgument("--headless");//Comment if we want to see the window. 
        options.AddArgument("--remote-allow-origins=*");

        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        EdgeDriver driver = null;

        try
        {
            driver = new EdgeDriver(path, options);
            driver.Navigate().GoToUrl(url);

            // Web Driver does not support returning the response code, but if
            // we got to here then it is most likely a 200 OK result
            _log.Information("{httpMethod} {uri}, {httpStatusCode} {httpStatusText}", "GET", driver.Url, 200, "OK");

            return (HttpStatusCode.OK, driver.PageSource, new Uri(driver.Url), "text/html");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "DriverGetString: Unexpected error '{message}'", ex.Message);
        }
        finally
        {
            if (driver != null)
            {
                driver.Close();
                driver.Quit();
            }
        }

        return (HttpStatusCode.InternalServerError, null, new Uri(url), "text/html");
    }

    public void SaveContentToDisk(string filename, bool removeScriptElements, string content)
    {
        // Delete the file if it already exists
        if (File.Exists(filename))
        {
            _log.Information("Delete existing file '{fileName}'", filename);
            File.Delete(filename);
        }

        if (filename.EndsWith(".html"))
        {
            // Load the Html into the DOM parser
            HtmlDocument doc = new();
            doc.LoadHtml(content);
            doc.OptionFixNestedTags = true;

            // List of html tags we really don't care to save
            var excludeHtmlTags = new List<string> { "style", "link", "svg", "form", "noscript", "button", "amp-ad" };
            if (removeScriptElements)
            {
                excludeHtmlTags.Add("script");
            }

            doc.DocumentNode
                .Descendants()
                .Where(n => excludeHtmlTags.Contains(n.Name))
                .ToList()
                .ForEach(n => n.Remove());

            _log.Information("Saving {bytes:N0} bytes to text file '{fileName}'", doc.DocumentNode.OuterLength, filename);
            doc.Save(filename);
        }
        else
        {
            _log.Information("Saving {bytes:N0} bytes to text file '{fileName}'", content.Length, filename);
            File.WriteAllText(filename, content);
        }
    }

    public void SaveContentToDisk(string filename, byte[] content)
    {
        // Delete the file if it already exists
        if (File.Exists(filename))
        {
            _log.Information("Delete existing file '{fileName}'", filename);
            File.Delete(filename);
        }

        try
        {
            _log.Information("SaveContentToDisk: Saving binary file '{fileName}' {bytes} bytes", filename, content.Length);
            File.WriteAllBytes(filename, content);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "SaveContentToDisk: Unexpected error '{message}'", ex.Message);
        }
    }

    public (bool, Uri) TrySaveUrlToDisk(string url, string urlHash, string filename, bool removeScriptElements = true)
    {
        // Delete the file if it already exists
        if (File.Exists(filename))
        {
            _log.Information("Delete existing file '{fileName}'", filename);
            File.Delete(filename);
        }

        if (!filename.EndsWith(".html") && !filename.EndsWith(".json") && !filename.EndsWith(".txt"))
        {
            SaveBinaryDataToDisk(url, urlHash, filename);
            return (false, new Uri(url));
        }

        bool retry = false;

        try
        {
            _log.Debug("Loading URL '{urlHash}':'{url}'", urlHash, url);
            (_, string content, Uri trueUri, _) = _crawler.GetString(url);

            if (trueUri is null)
            {
                _log.Warning("TrySaveUrlToDisk: Failure to crawl url '{url}'", url);
                return (true, new Uri(url));
            }

            if (filename.EndsWith(".html"))
            {
                // Load the Html into the DOM parser
                HtmlDocument doc = new();
                doc.LoadHtml(content);
                doc.OptionFixNestedTags = true;

                // List of html tags we really don't care to save
                var excludeHtmlTags = new List<string> { "style", "link", "svg", "form", "noscript", "amp-ad" };
                if (trueUri.AbsoluteUri.Contains("apnews.com") || trueUri.AbsoluteUri.Contains("rumble.com"))
                {
                    removeScriptElements = false;
                }
                if (removeScriptElements)
                {
                    excludeHtmlTags.Add("script");
                }

                doc.DocumentNode
                    .Descendants()
                    .Where(n => excludeHtmlTags.Contains(n.Name))
                    .ToList()
                    .ForEach(n => n.Remove());

                _log.Information("Saving {bytes:N0} bytes to text file '{fileName}'", doc.DocumentNode.OuterLength, filename);
                doc.Save(filename);
            }
            else
            {
                _log.Information("Saving {bytes:N0} bytes to text file '{fileName}'", content.Length, filename);
                File.WriteAllText(filename, content);
            }

            return (retry, trueUri);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "SaveUrlToDisk: Unexpected error '{message}'", ex.Message);
            retry = ex.Message.Contains("Moved") || ex.Message.Contains("Request timed out");
        }

        return (retry, new Uri(url));
    }

    private void SaveBinaryDataToDisk(string url, string urlHash, string filename)
    {
        try
        {
            _log.Debug("Loading binary URL '{urlHash}':'{url}'", urlHash, url);
            var fileBytes = _crawler.DownloadData(url);

            // if the remote file was found, download it
            _log.Information("Saving binary file '{fileName}' {bytes} bytes", filename, fileBytes.Length);
            File.WriteAllBytes(filename, fileBytes);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "SaveBinaryDataToDisk: Unexpected error '{message}'", ex.Message);
        }
    }

    public Uri WebDriverUrlToDisk(string url, string filename)
    {
        _log.Information("WebDriverClient GetString to {url}", url);

        var options = new EdgeOptions();
        options.AddArgument("headless");//Comment if we want to see the window. 

        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        EdgeDriver driver = null;

        try
        {
            driver = new EdgeDriver(path, options);
            driver.Navigate().GoToUrl(url);

            // Web Driver does not support returning the response code, but if
            // we got to here then it is most likely a 200 OK result
            _log.Information("{httpMethod} {uri}, {httpStatusCode} {httpStatusText}", "GET", driver.Url, 200, "OK");

            // Delete the file if it already exists
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            _log.Information("Saving {bytes:N0} bytes to text file '{fileName}'", driver.PageSource.Length, filename);
            File.WriteAllText(filename, driver.PageSource);

            return new Uri(driver.Url);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "WebDriverClient: Unexpected error '{message}'", ex.Message);
        }
        finally
        {
            if (driver != null)
            {
                driver.Close();
                driver.Quit();
            }
        }

        return new Uri(url);
    }

    public void SaveThumbnailToDisk(string url, string filename)
    {
        var options = new EdgeOptions();
        options.AddArgument("headless");//Comment if we want to see the window. 

        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        EdgeDriver driver = null;

        try
        {
            driver = new EdgeDriver(path, options);
            driver.Manage().Window.Size = new System.Drawing.Size(2000, 4000);
            driver.Navigate().GoToUrl(url);
            Thread.Sleep(5000);
            var screenshot = (driver as ITakesScreenshot).GetScreenshot();

            _log.Information("Saving thumbnail file '{filename}'", filename);
            screenshot.SaveAsFile(filename);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "ERROR: Unable to save webpage thumbnail");
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
        if (!Uri.TryCreate(defaultBaseUrl, UriKind.Absolute, out Uri baseUri))
        {
            _log.Warning("Invalid base url {baseUrl}, aborting relative Url fixup", defaultBaseUrl);
            return pathAndQuery;
        }

        return RepairUrl(pathAndQuery, baseUri);
    }

    public string RepairUrl(string relativeUrl, Uri baseUri)
    {
        if (!Uri.TryCreate(baseUri, relativeUrl, out Uri absoluteUri))
        {
            _log.Warning("Invalid relative url {relativeUrl}, aborting relative Url fixup", relativeUrl);
            return relativeUrl;
        }

        _log.Information("Repaired link '{relativeUrl}' to '{absoluteUri}'", relativeUrl, absoluteUri);
        return absoluteUri.AbsoluteUri;
    }

    public (HttpStatusCode, string, Uri, string) DownloadString(string url)
    {
        return _crawler.GetString(url);
    }

    public (HttpStatusCode, Uri, string) GetContentType(string url)
    {
        _log.Debug("GetContentType for {url}", url);

        try
        {
            return _crawler.GetContentType(url);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "GetContentType: Unexpected error '{message}'", ex.Message);
            return (HttpStatusCode.InternalServerError, default, null);
        }
    }
}
