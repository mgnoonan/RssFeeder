using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.TagParsers
{
    public class JsonLdTagParser : ITagParser
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

            Log.Information("Attempting json+ld tag parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

            // Query the document by CSS selectors to get the article text
            var elements = document.QuerySelectorAll("script");
            if (elements.Length == 0)
            {
                Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", "script");
                return "<p>Error reading article: 'script' article body selector not found.</p>";
            }

            foreach (var element in elements)
            {
                if (element.HasAttribute("type") && element.GetAttribute("type") == "application/ld+json")
                {
                    return element.TextContent;
                }
            }

            Log.Warning("Error reading article: No ld+json selector not found.");
            return string.Empty;
        }
    }
}
