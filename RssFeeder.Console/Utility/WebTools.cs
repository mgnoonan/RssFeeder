using System.Net;
using System.Text;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// Summary description for WebTools.
    /// </summary>
    public class WebTools
    {
        public WebTools() { }

        public static string GetUrl(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4170.0 Safari/537.36 Edg/85.0.552.1");
                return client.DownloadString(url);
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
        public static string RepairUrl(string pathAndQuery)
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
