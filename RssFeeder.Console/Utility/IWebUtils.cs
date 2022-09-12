namespace RssFeeder.Console.Utility;

public interface IWebUtils
{
    (HttpStatusCode, string, Uri, string) DownloadString(string url);
    (bool, Uri) TrySaveUrlToDisk(string url, string urlHash, string filename, bool removeScriptElements = true);
    Uri WebDriverUrlToDisk(string url, string filename);
    void SaveThumbnailToDisk(string url, string filename);
    string RepairUrl(string pathAndQuery, string defaultBaseUrl);
    (HttpStatusCode, Uri, string) GetContentType(string url);
}
