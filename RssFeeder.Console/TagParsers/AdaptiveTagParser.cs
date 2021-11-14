using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.Parsers
{
    public class AdaptiveTagParser : ITagParser
    {
        public string ParseTagsBySelector(string html, SiteArticleDefinition options)
        {
            return ParseTagsBySelector(html, "", "p");
        }

        public string ParseTagsBySelector(string html, string bodySelector, string paragraphSelector)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            Log.Information("Attempting adaptive parsing using paragraph selector '{paragraphSelector}'", paragraphSelector);

            // Query the document by CSS selectors to get the article text
            var paragraphs = document.QuerySelectorAll(paragraphSelector);
            if (!paragraphs.Any())
            {
                Log.Warning("Paragraph selector '{paragraphSelector}' not found", paragraphSelector);
                return string.Empty;
            }

            var dict = new Dictionary<string, int>();
            foreach (var p in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(System.Web.HttpUtility.HtmlDecode(p.TextContent)))
                {
                    continue;
                }

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
            foreach (var key in dict.Keys)
            {
                if (dict[key] > highCount)
                {
                    bodySelector = key;
                    highCount = dict[key];
                }
            }

            Log.Information("Found {totalCount} paragraph selectors '{paragraphSelector}' in html body", paragraphs.Count(), paragraphSelector);
            Log.Information("Parent with the most paragraph selectors is '{bodySelector}':{highCount}", bodySelector, highCount);

            if (highCount == 1)
            {
                Log.Warning("Only 1 paragraph selector found, that doesn't count");
                return string.Empty;
            }

            try
            {
                // Query the document by CSS selectors to get the article text
                var container = document.QuerySelector(bodySelector);

                // Get only the paragraphs under the parent
                var paragraphs2 = container.QuerySelectorAll(paragraphSelector);

                return BuildArticleText(paragraphs2);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing paragraph selectors '{paragraphSelector}', '{message}'", paragraphSelector, ex.Message);
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
