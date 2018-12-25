using System.Security.Cryptography;
using System.Text;

namespace RssFeederFunctionApp.Utils
{
    public class WebTools
    {
        /// <summary>
        /// Transform the incoming url to an MD5 hash code
        /// </summary>
        /// <param name="url">The url to transform</param>
        /// <returns>A 32 character hash code</returns>
        public static string CreateMD5Hash(string url)
        {
            char[] cs = url.ToLowerInvariant().ToCharArray();
            byte[] buffer = new byte[cs.Length];
            for (int i = 0; i < cs.Length; i++)
                buffer[i] = (byte)cs[i];

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] output = md5.ComputeHash(buffer);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < output.Length; i++)
                builder.AppendFormat("{0:x2}", output[i]);

            return builder.ToString();
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
