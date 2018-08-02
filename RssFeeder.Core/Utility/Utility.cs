using System.Security.Cryptography;
using System.Text;

namespace RssFeeder.Core.Utility
{
    /// <summary>
    /// Summary description for Utility.
    /// </summary>
    public class Utility
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

            MD5 md5 = MD5.Create();
            byte[] output = md5.ComputeHash(buffer);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < output.Length; i++)
                builder.AppendFormat("{0:x2}", output[i]);

            return builder.ToString();
        }
    }
}
