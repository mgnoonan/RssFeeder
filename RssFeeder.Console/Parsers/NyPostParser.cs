using System.Text;
using AngleSharp.Parser.Html;

namespace RssFeeder.Console.Parsers
{
    class NyPostParser : ISiteParser
    {
        public string GetArticleText(string html)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            // Query the document by CSS selectors to get the article text
            var paragraphs = document.QuerySelector(".entry-content").QuerySelectorAll("p");

            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                if (p.TagName.Equals("P"))
                {
                    description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
                }
                else
                {
                    description.AppendLine($"<ul>{p.InnerHtml}</ul>");
                }
            }

            return description.ToString();
        }
    }
}
