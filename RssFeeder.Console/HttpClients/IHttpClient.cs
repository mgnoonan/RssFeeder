using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.HttpClients
{
    public interface IHttpClient
    {
        string GetString(string url);
        byte[] DownloadData(string url);
        string GetContentType(string url);
    }
}
