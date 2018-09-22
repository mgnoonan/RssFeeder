using System.Text;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using RssFeeder.Models;
using RssFeeder.Console.Parsers;

namespace RssFeeder.Console.ArticleParsers
{
    class JsonParser : IArticleParser
    {
        public string GetArticleBySelector(string html, SiteArticleDefinition options)
        {
            // Find the JSON slug
            string json = GetJsonSlug(html, options.ArticleSelector);
            var story = JsonConvert.DeserializeObject<StoryModel>(json);

            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse($"<html><body>{story.articles[0].body}</body></html>");

            // Query the document by CSS selectors to get the article text
            var container = document.QuerySelector("body");
            if (container == null)
            {
                return $"<p>Error reading article: '{options.ArticleSelector}' article selector not found.</p>";
            }

            var paragraphs = container.QuerySelectorAll(options.ParagraphSelector);

            return BuildArticleText(paragraphs);
        }

        private string BuildArticleText(IHtmlCollection<IElement> paragraphs)
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

        private string GetJsonSlug(string html, string variable)
        {
            string slugMarker = $"{variable} = ";
            int start = html.IndexOf(slugMarker) + slugMarker.Length;
            int length = html.IndexOf("};", start) - start + 1;

            return html.Substring(start, length).Trim();
        }
    }
}
