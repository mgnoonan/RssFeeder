namespace RssFeeder.Console.Utility
{
    public interface IWebUtils
    {
        string DownloadString(string url);
        string DownloadStringWithCompression(string url);
        string SaveUrlToDisk(string url, string urlHash, string filename);
        void SaveThumbnailToDisk(string url, string filename);
        string StripJavascriptAndCss(string text);
        string RepairUrl(string pathAndQuery);
    }
}
