namespace RssFeeder.Console.Utility;

public interface IWebUtils
{
    (string, Uri) DownloadString(string url);
    (string, Uri) SaveUrlToDisk(string url, string urlHash, string filename, bool removeScriptElements = true);
    (string, Uri) WebDriverUrlToDisk(string url, string urlHash, string filename);
    void SaveThumbnailToDisk(string url, string filename);
    string RepairUrl(string pathAndQuery, string defaultBaseUrl);
    string GetContentType(string url);
}
