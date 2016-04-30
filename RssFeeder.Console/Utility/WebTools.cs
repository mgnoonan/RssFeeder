using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RssFeeder.Console.Utility
{
	/// <summary>
	/// Summary description for WebTools.
	/// </summary>
	public class WebTools
	{
		public WebTools()	{}

		public static string GetUrl(string url)
		{
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.152 Safari/537.36");
                return client.DownloadString(url);
            }
			//String result;

			//// Read the response using a stream reader
			//using ( StreamReader sr = new StreamReader(GetUrlResponse(url)) )
			//{
			//	result = sr.ReadToEnd();
			//	sr.Close();
			//}

			//return result;
		}

		//public static Stream GetUrlResponse(string url)
		//{
  //          return GetUrlResponse(new Uri(url));
		//}

        public static Stream GetUrlResponse(Uri requestUri)
        {
            WebResponse response;

            // Open up the URL with a request object
            WebRequest request = System.Net.HttpWebRequest.Create(requestUri);
            response = request.GetResponse();

            // Read the response using a stream reader
            return response.GetResponseStream();
        }
	
		/// <summary>
		/// Removes all HTML markup from a string
		/// </summary>
		/// <param name="sText">The string containing HTML source</param>
		/// <returns>The text without any markup</returns>
		public static string StripHTML(string sText)
		{
			String result;

			// Strip out HTML tags
			result = Regex.Replace(sText, "<[^>]*>", " ");

			// Replace HTML constructs
			result = result.Replace("&nbsp;", " ");
			result = result.Replace("&#38;", "&");
			result = result.Replace("&#63;", "?");

			// Replace whitespace
			result = result.Replace("\x09", " ");
			result = result.Replace("\x0A\x0A", "");
			result = result.Replace("\x0A", "\x0D\x0A");
			result = result.Replace("  ", " ");
			
			return result;
		}

		public static void SplitHREF(string html, out string href, out string text)
		{
			int startPos = html.ToLower().IndexOf("<a href");
			int endPos = html.ToLower().IndexOf("</a>");

			string search = "href=\"";
			int hrefPos = html.ToLower().IndexOf(search) + search.Length;
			href = html.Substring(hrefPos, html.IndexOf("\"", hrefPos) - hrefPos).Trim();

			startPos = html.IndexOf(">", startPos) + 1;
			text = html.Substring(startPos, endPos - startPos);
			text = StripHTML(text).Trim();
		}
	
		public static string MakeFullURL(string baseURL, string filePath)
		{
			StringBuilder sb = new StringBuilder();

			if(baseURL.Trim().Length > 0)
			{
				sb.Append(baseURL.Trim());

				if(!baseURL.EndsWith("/"))
					sb.Append("/");
			}

			if(filePath.StartsWith("/"))
				sb.Append(filePath.Trim().Substring(1));
			else
				sb.Append(filePath.Trim());

			return sb.ToString();
		}
	}
}
