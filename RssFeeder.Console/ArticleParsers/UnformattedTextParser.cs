﻿using System.Text;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using RssFeeder.Models;

namespace RssFeeder.Console.Parsers
{
    public class UnformattedTextParser : IArticleParser
    {
        public string GetArticleBySelector(string html, SiteArticleDefinition options)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            // Query the document by CSS selectors to get the article text
            var container = document.QuerySelector(options.ArticleSelector);
            if (container == null)
            {
                return $"<p>Error reading article: '{options.ArticleSelector}' article selector not found.</p>";
            }

            if (string.IsNullOrWhiteSpace(options.ParagraphSelector))
            {
                return $"<p><pre>{container.InnerHtml.Trim()}</pre></p>";
            }
            else
            {
                var paragraphs = container.QuerySelectorAll(options.ParagraphSelector);
                return BuildArticleText(paragraphs);
            }
        }

        protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
        {
            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                description.AppendLine($"<p><pre>{p.TextContent.Trim()}</pre></p>");
            }

            return description.ToString();
        }
    }
}