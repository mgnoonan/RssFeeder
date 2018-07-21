﻿using System.Text;
using AngleSharp.Parser.Html;

namespace RssFeeder.Console.Parsers
{
    class DailyMailParser : ISiteParser
    {
        public string GetArticleText(string html)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            StringBuilder description = new StringBuilder();

            // Query the document by CSS selectors to get the article text
            var bullets = document.QuerySelectorAll(".mol-bullets-with-font");

            foreach (var b in bullets)
            {
                description.AppendLine($"<ul>{b.InnerHtml}</ul>");
            }

            var paragraphs = document.QuerySelector("#js-article-text").QuerySelectorAll("p.mol-para-with-font");

            foreach (var p in paragraphs)
            {
                description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
            }

            return description.ToString();
        }
    }
}
