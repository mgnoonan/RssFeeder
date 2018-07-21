using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;

namespace RssFeeder.Console.Parsers
{
    class VarietyParser : ISiteParser
    {
        public string GetArticleText(string html)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            StringBuilder description = new StringBuilder();

            // Query the document by CSS selectors to get the article text
            var paragraphs = document.QuerySelector(".c-content").QuerySelectorAll("p");

            foreach (var p in paragraphs)
            {
                if (!p.TextContent.Contains("RELATED VIDEO:"))
                {
                    description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
                }
            }

            return description.ToString();
        }
    }
}
