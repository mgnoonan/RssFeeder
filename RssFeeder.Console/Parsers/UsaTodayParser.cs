using System.Text;
using AngleSharp.Parser.Html;

namespace RssFeeder.Console.Parsers
{
    class UsaTodayParser : ISiteParser
    {
        public string GetArticleText(string html)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            // Query the document by CSS selectors to get the article text
            var paragraphs = document.QuerySelector(".story").QuerySelectorAll("p.p-text,h2.presto-h2");

            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                if (p.TagName.Equals("P"))
                {
                    if (p.TextContent.StartsWith("More:") || p.TextContent.StartsWith("Related:"))
                    {
                        // Skip
                    }
                    else
                    {
                        description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
                    }
                }
                else
                {
                    description.AppendLine($"<h4>{p.TextContent.Trim()}</h4>");
                }
            }

            return description.ToString();
        }
    }
}
