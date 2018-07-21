﻿using System.Text;
using AngleSharp.Parser.Html;

namespace RssFeeder.Console.Parsers
{
    class ReutersParser : ISiteParser
    {
        public string GetArticleText(string html)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            // Query the document by CSS selectors to get the article text
            var paragraphs = document.QuerySelectorAll(".StandardArticleBody_body > p");

            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
            }

            return description.ToString();
        }
    }
}
