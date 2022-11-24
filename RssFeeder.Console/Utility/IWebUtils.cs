namespace RssFeeder.Console.Utility;

public interface IWebUtils
{
    (HttpStatusCode status, string content, Uri trueUri, string contentType) ClientGetString(string url);
    byte[] ClientGetBytes(string url);
    (HttpStatusCode status, string content, Uri trueUri, string contentType) DriverGetString(string url);
    void SaveContentToDisk(string filename, bool removeScriptElements, string content);
    void SaveContentToDisk(string filename, byte[] content);
    (HttpStatusCode, string, Uri, string) DownloadString(string url);
    (bool, Uri) TrySaveUrlToDisk(string url, string urlHash, string filename, bool removeScriptElements = true);
    Uri WebDriverUrlToDisk(string url, string filename);
    void SaveThumbnailToDisk(string url, string filename);
    string RepairUrl(string pathAndQuery, string defaultBaseUrl);
    string RepairUrl(string relativeUrl, Uri baseUri);
    (HttpStatusCode, Uri, string) GetContentType(string url);
}
