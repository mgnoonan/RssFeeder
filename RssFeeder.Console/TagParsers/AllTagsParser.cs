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
    public class AllTagsParser : ITagParser
    {
        public string ParseTagsBySelector(string html, SiteArticleDefinition options)
        {
            return ParseTagsBySelector(html, options.ArticleSelector, options.ParagraphSelector);
        }

        public string ParseTagsBySelector(string html, string bodySelector, string paragraphSelector)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            Log.Information("Attempting alltags parsing using paragraph selector '{paragraphSelector}'", paragraphSelector);

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

            Log.Information("Found {totalCount} paragraph selectors '{paragraphSelector}' in html body", paragraphs.Count(), paragraphSelector);

            try
            {
                return BuildArticleText(paragraphs);
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
