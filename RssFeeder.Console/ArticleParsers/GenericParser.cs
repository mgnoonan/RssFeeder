﻿using System;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using RssFeeder.Console.Models;

namespace RssFeeder.Console.Parsers
{
    public class GenericParser : IArticleParser
    {
        public string GetArticleBySelector(string html, SiteArticleDefinition options)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            // Query the document by CSS selectors to get the article text
            var container = document.QuerySelector(options.ArticleSelector);
            var paragraphs = container.QuerySelectorAll(options.ParagraphSelector);

            return BuildArticleText(paragraphs);
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
