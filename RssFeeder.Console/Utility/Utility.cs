using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using System.Web;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// Summary description for Utility.
    /// </summary>
    public class Utility
    {
        public Utility() { }

        public static string CleanString(string value, string pattern)
        {
            return CleanString(value, pattern, string.Empty);
        }

        public static string CleanString(string value, string pattern, string replacement)
        {
            return CleanString(value, pattern, string.Empty, RegexOptions.IgnoreCase);
        }

        public static string CleanString(string value, string pattern, string replacement, RegexOptions options)
        {
            return Regex.Replace(value, pattern, replacement, options);
        }

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

        public static string GetParagraphTagsFromHtml(string url)
        {
            string description = string.Empty;

            try
            {
                // Load the initial HTML from the URL
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument doc = hw.Load(url);

                // Sanitize the markup and reload back to the HtmlDocument
                doc.LoadHtml(GetSanitizedMarkup(doc.DocumentNode.InnerHtml));

                // Grab all the paragraph tags and just append the innnerText
                var paragraphTags = doc.DocumentNode.SelectNodes("//p");
                foreach (var paragraph in paragraphTags)
                {
                    string innerText = System.Web.HttpUtility.HtmlDecode(paragraph.InnerText).Trim();
                    innerText = RemoveWhitespaceWithSplit(innerText).Trim();
                    if (!string.IsNullOrWhiteSpace(innerText) && CharCount(innerText, ";{}=|") < 3 && CharCount(innerText, ' ') >= 5)
                    {
                        description += "<p>" + innerText + "</p>" + Environment.NewLine;
                    }
                }
            }
            catch { }

            return description;
        }

        public static string GetSanitizedMarkup(string html)
        {
            return MarkupSanitizer.Sanitizer.SanitizeMarkup(html).MarkupText;
        }

        internal static int CharCount(string inputText, string charList)
        {
            return CharCount(inputText, charList.ToCharArray());
        }

        internal static int CharCount(string inputText, char[] charList)
        {
            int count = 0;

            foreach (char c in charList)
            {
                count += CharCount(inputText, c);
            }

            return count;
        }

        internal static int CharCount(string inputText, char charToCount)
        {
            int count = 0;

            foreach (char c in inputText.ToCharArray())
            {
                if (c.CompareTo(charToCount) == 0)
                    count++;
            }

            return count;
        }

        internal static string RemoveWhitespaceWithSplit(string inputText)
        {
            var sb = new StringBuilder();

            string[] parts = inputText.Split(new char[] { ' ', '\n', '\t', '\r', '\f', '\v' }, StringSplitOptions.RemoveEmptyEntries);

            int size = parts.Length;
            for (int i = 0; i < size; i++)
                sb.AppendFormat("{0} ", parts[i]);

            return sb.ToString();
        }

        public static void GetDescriptionAndImageFromMeta(string url, ref string description, ref string imageUrl)
        {
            string title = string.Empty;
            string contentType = string.Empty;
            string keywords = string.Empty;

            try
            {
                //WebClient client = new WebClient();
                //string html = client.DownloadString(url);
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument doc = hw.Load(url);
                string html = doc.DocumentNode.InnerHtml;

                bool result = ParseMetaTags(doc, ref contentType, ref title, ref keywords, ref description, ref imageUrl);

                // If the agility pack fails for some reason, 
                // fall back to the old Regex way
                if (!result)
                    ParseMetaTags(html, ref contentType, ref title, ref keywords, ref description, ref imageUrl);
            }
            catch (Exception ex)
            {
                description = string.Format("{0}\r\n{1}", ex.Message, ex.StackTrace);
                imageUrl = string.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="metaContentType"></param>
        /// <param name="metaTitle"></param>
        /// <param name="metaKeywords"></param>
        /// <param name="metaDescription"></param>
        /// <param name="metaImage"></param>
        /// <returns></returns>
        /// <remarks>
        /// http://www.4guysfromrolla.com/articles/011211-1.aspx
        /// </remarks>
        public static bool ParseMetaTags(HtmlDocument doc, ref string metaContentType, ref string metaTitle,
                           ref string metaKeywords, ref string metaDescription, ref string metaImage)
        {
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            var metaTags = doc.DocumentNode.SelectNodes("//meta");

            if (titleNode != null)
                metaTitle = HttpUtility.HtmlDecode(titleNode.InnerText);

            if (metaTags == null)
                return false;

            foreach (var tag in metaTags)
            {
                var attribute = tag.Attributes["name"];
                if (attribute == null)
                    continue;

                string name = attribute.Value;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                switch (name.ToLowerInvariant())
                {
                    case "keywords":
                        metaKeywords = tag.Attributes["content"].Value;
                        break;
                    case "description":
                        metaDescription = HttpUtility.HtmlDecode(tag.Attributes["content"].Value);
                        break;
                    case "title":
                        if (string.IsNullOrWhiteSpace(metaTitle))
                        {
                            metaTitle = HttpUtility.HtmlDecode(tag.Attributes["content"].Value);
                        }
                        break;
                    case "og:image":
                        metaImage = tag.Attributes["content"].Value;
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="html"></param>
        /// <param name="metaContentType"></param>
        /// <param name="metaTitle"></param>
        /// <param name="metaKeywords"></param>
        /// <param name="metaDescription"></param>
        /// <returns></returns>
        /// <remarks>
        /// http://codeasp.net/blogs/teisenhauer/microsoft-net/170/parse-meta-tags-in-c-sharp
        /// </remarks>
        public static bool ParseMetaTags(string html, ref string metaContentType, ref string metaTitle,
                                   ref string metaKeywords, ref string metaDescription, ref string metaImage)
        {
            try
            {
                html = stripCrLf(html);

                // --- Parse the title
                Match m = Regex.Match(html, "<title>([^<]*)</title>", RegexOptions.IgnoreCase);
                metaTitle = m.Groups[1].Value;

                // --- Retrieve all the meta tags into one collection
                var metaTags = Regex.Matches(html, "<meta[^>]+>", RegexOptions.IgnoreCase);
                foreach (Match tag in metaTags)
                {
                    if (Regex.IsMatch(tag.Value, "name=\"?keywords\"?", RegexOptions.IgnoreCase))
                    {
                        m = Regex.Match(tag.Value, "content=\"?([^<]*)\"?", RegexOptions.IgnoreCase);
                        metaKeywords = m.Groups[m.Groups.Count - 1].Value.StripAfter("\"");
                    }
                    if (Regex.IsMatch(tag.Value, "name=\"?description\"?", RegexOptions.IgnoreCase))
                    {
                        m = Regex.Match(tag.Value, "content=\"?([^<]*)\"?", RegexOptions.IgnoreCase);
                        metaDescription = HttpUtility.HtmlDecode(m.Groups[m.Groups.Count - 1].Value.StripAfter("\""));
                    }
                    if (Regex.IsMatch(tag.Value, "name=\"?title\"?", RegexOptions.IgnoreCase) &&
                        metaTitle.IsNullOrWhiteSpace())
                    {
                        m = Regex.Match(tag.Value, "content=\"?([^<]*)\"?", RegexOptions.IgnoreCase);
                        metaTitle = HttpUtility.HtmlDecode(m.Groups[m.Groups.Count - 1].Value.StripAfter("\""));
                    }
                    if (Regex.IsMatch(tag.Value, "property=\"?og:image\"?", RegexOptions.IgnoreCase))
                    {
                        m = Regex.Match(tag.Value, "content=\"?([^<]*)\"?", RegexOptions.IgnoreCase);
                        metaImage = m.Groups[m.Groups.Count - 1].Value.StripAfter("\"");
                    }
                    if (Regex.IsMatch(tag.Value, "http-equiv=\"?content-type\"?", RegexOptions.IgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(metaContentType))
                            metaContentType += " | [Meta] ";

                        m = Regex.Match(tag.Value, "content=\"?([^<]*)\"?", RegexOptions.IgnoreCase);
                        metaContentType += m.Groups[m.Groups.Count - 1].Value.StripAfter("\"");
                    }
                }

                return true;
            }
            catch
            {
                // do something with the error
                return false;
            }
        }

        public static string stripCrLf(string text, string replacementString = "")
        {
            string pattern = @"[\n\r]";
            Regex re = new Regex(pattern, RegexOptions.IgnoreCase);

            return re.Replace(text, replacementString);
        }

        public static void CreateThumbnail(Uri imageUri, string targetFileName, int maxWidth, int maxHeight)
        {
            if (imageUri == null)
                throw new ArgumentNullException("imageLink");
            if (string.IsNullOrWhiteSpace(targetFileName))
                throw new ArgumentNullException("targetFileName");

            using (var image = Image.FromStream(WebTools.GetUrlResponse(imageUri)))
            {
                Rectangle r = CalculateRectangle(image, maxWidth, maxHeight);

                using (var thumbnail = image.GetThumbnailImage(r.Width, r.Height, null, new IntPtr()))
                {
                    thumbnail.Save(targetFileName, image.RawFormat);
                }
            }
        }

        private static Rectangle CalculateRectangle(Image image, int maxWidth, int maxHeight)
        {
            double aspectRatio = (double)image.Width / (double)image.Height;
            Rectangle r = new Rectangle(0, 0, image.Width, image.Height);

            if (image.Width > maxWidth)
            {
                int targetWidth = maxWidth;
                int targetHeight = (int)Math.Floor(image.Height / aspectRatio);
                r.Width = targetWidth;
                r.Height = targetHeight;
            }

            return r;
        }
    }
}
