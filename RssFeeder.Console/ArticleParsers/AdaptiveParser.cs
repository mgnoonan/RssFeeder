using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Raven.Client.Documents.Smuggler;
using RssFeeder.Console.Parsers;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.ArticleParsers
{
    public class AdaptiveParser : IArticleParser
    {
        public string GetArticleBySelector(string html, SiteArticleDefinition options)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // Query the document by CSS selectors to get the article text
            var paragraphs = document.QuerySelectorAll("p");
            if (!paragraphs.Any())
            {
                Log.Warning("No paragraphs found, probably the content is blocked");
                return string.Empty;
            }

            var dict = new Dictionary<string, int>();
            foreach (var p in paragraphs)
            {
                var parent = p.ParentElement;
                var key = parent.GetSelector();

                if (dict.ContainsKey(key))
                {
                    dict[key]++;
                }
                else
                {
                    dict.Add(key, 1);
                }
            }

            // Get the parent with the most paragraphs, it should be the article content
            int highCount = default;
            string highKey = "";
            foreach(var key in dict.Keys)
            {
                if (dict[key] > highCount)
                {
                    highKey = key;
                    highCount = dict[key];
                }
            }

            Log.Information("Found {totalCount} paragraphs in article", paragraphs.Count());
            Log.Information("Parent with the most paragraphs is '{highClassName}':{highCount}", highKey, highCount);

            if (highCount == 1)
            {
                Log.Warning("Only 1 paragraph found, that doesn't count");
                return string.Empty;
            }

            try
            {
                // Query the document by CSS selectors to get the article text
                var container = document.QuerySelector(highKey);

                // Get only the paragraphs under the parent
                var paragraphs2 = container.QuerySelectorAll("p");

                return BuildArticleText(paragraphs2);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error selecting paragraphs '{message}'", ex.Message);
            }

            return string.Empty;
        }

        protected virtual string BuildArticleText(IHtmlCollection<IElement> paragraphs)
        {
            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                if (p.TagName.ToLower().StartsWith("h"))
                {
                    description.AppendLine($"<h4>{p.TextContent.Trim()}</h4>");
                }
                else
                {
                    description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
                }
            }

            return description.ToString();
        }
    }
}
