using System.Net;
using System.Text;

namespace RssFeeder.Core.Utility
{
    /// <summary>
    /// Summary description for WebTools.
    /// </summary>
    public class WebTools
    {
        public static string GetUrl(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.152 Safari/537.36");
                return client.DownloadString(url);
            }
        }

        public static string MakeFullURL(string baseURL, string filePath)
        {
            StringBuilder sb = new StringBuilder();

            if (baseURL.Trim().Length > 0)
            {
                sb.Append(baseURL.Trim());

                if (!baseURL.EndsWith("/"))
                    sb.Append("/");
            }

            if (filePath.StartsWith("/"))
                sb.Append(filePath.Trim().Substring(1));
            else
                sb.Append(filePath.Trim());

            return sb.ToString();
        }
    }
}
