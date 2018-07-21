using System;
using System.Text;
using HtmlAgilityPack;
using log4net;
using RssFeeder.Console.Models;

namespace RssFeeder.Console.Parsers
{
    public class GenericParser : ISiteParser
    {
        private HtmlDocument _document = null;
        private FeedItem _item = null;
        private ILog _log = null;

        public GenericParser(ILog log)
        {
            _log = log;
        }

        public string GetArticleText(string html)
        {
            throw new NotImplementedException();
        }

        public void Load(FeedItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            HtmlWeb hw = new HtmlWeb();
            hw.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.152 Safari/537.36";
            HtmlDocument doc = hw.Load(item.Url);

            // Sanitize the markup and reload back to the HtmlDocument
            try
            {
                string sanitizedMarkup = Utility.Utility.GetSanitizedMarkup(doc.DocumentNode.InnerHtml);
                _document.LoadHtml(sanitizedMarkup);
            }
            catch (Exception ex)
            {
                _log.Error("Unable to sanitize markup", ex);
                _document = doc;
            }

            _item = item;
        }

        public void Parse()
        {
            StringBuilder sb = new StringBuilder();

            // Grab all the paragraph tags and just append the innnerText
            var paragraphTags = _document.DocumentNode.SelectNodes("//p");
            foreach (var paragraph in paragraphTags)
            {
                string innerText = System.Web.HttpUtility.HtmlDecode(paragraph.InnerText).Trim();
                innerText = Utility.Utility.RemoveWhitespaceWithSplit(innerText).Trim();
                if (!string.IsNullOrWhiteSpace(innerText) && Utility.Utility.CharCount(innerText, ";{}=|") < 3 && Utility.Utility.CharCount(innerText, ' ') >= 5)
                {
                    sb.AppendFormat("<p>{0}</p>", innerText);
                    sb.AppendLine();
                }
            }

            _item.Description = sb.ToString();
        }
    }
}
