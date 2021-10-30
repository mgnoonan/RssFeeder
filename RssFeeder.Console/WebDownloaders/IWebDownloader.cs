using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.WebDownloaders
{
    public interface IWebDownloader
    {
        string GetString(string url);
        byte[] DownloadData(string url);
        string GetContentType(string url);
    }
}
