using System.Threading.Tasks;

namespace RssFeeder.Console.Utility
{
    public interface IWebUtils
    {
        string DownloadStringWithCompression(string url);
        Task<string> DownloadStringWithCompressionAsync(string url);
        string SaveUrlToDisk(string url, string urlHash, string filename, bool removeScriptElements = true);
        string WebDriverUrlToDisk(string url, string urlHash, string filename);
        void SaveThumbnailToDisk(string url, string filename);
        string RepairUrl(string pathAndQuery);
        string GetContentType(string url);
    }
}
