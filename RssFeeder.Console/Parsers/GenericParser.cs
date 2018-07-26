using System;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;

namespace RssFeeder.Console.Parsers
{
    public class GenericParser : ISiteParser
    {
        public string GetArticleBySelector(string html, string articleSelector, string paragraphSelector)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            // Query the document by CSS selectors to get the article text
            var paragraphs = document.QuerySelector(articleSelector).QuerySelectorAll(paragraphSelector);

            return BuildArticleText(paragraphs);
        }

        public string GetArticleText(string html)
        {
            throw new NotImplementedException();
        }

        protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
        {
            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
            }

            return description.ToString();
        }
    }
}
