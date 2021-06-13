using System;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using RssFeeder.Console.Parsers;
using RssFeeder.Models;
using Serilog;

namespace RssFeeder.Console.ArticleParsers
{
    public class ScriptParser : IArticleParser
    {
        public string GetArticleBySelector(string html, SiteArticleDefinition options)
        {
            return GetArticleBySelector(html, options.ArticleSelector, options.ParagraphSelector);
        }

        public string GetArticleBySelector(string html, string bodySelector, string paragraphSelector)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            Log.Information("Attempting script block parsing using body selector '{bodySelector}' and paragraph selector '{paragraphSelector}'", bodySelector, paragraphSelector);

            // Query the document by CSS selectors to get the article text
            var blocks = document.QuerySelectorAll("script");
            if (blocks.Count() == 0)
            {
                Log.Warning("Error reading article: '{bodySelector}' article body selector not found.", bodySelector);
                return $"<p>Error reading article: '{bodySelector}' article body selector not found.</p>";
            }

            string content = "";
            foreach (var block in blocks)
            {
                if (block.TextContent.Contains(bodySelector))
                {
                    var lines = block.TextContent.Replace("\\u003c", "<").Split("\n", StringSplitOptions.RemoveEmptyEntries);
                    content = lines.Where(q => q.Contains(bodySelector)).FirstOrDefault();
                    break;
                }
            }

            try
            {
                var groups = Regex.Match(content, "\"urn:publicid:ap\\.org:[a-f0-9]*\":\\s?({.*\"canonicalUrl\":\\s?\"[-a-z0-9]*\"\\s?})\\s?},").Groups;
                var jsonRaw = groups[1].Value;
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonRaw);

                return jsonObject.storyHTML;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing paragraph selectors '{paragraphSelector}', '{message}'", paragraphSelector, ex.Message);
            }

            return string.Empty;
        }
    }
}
