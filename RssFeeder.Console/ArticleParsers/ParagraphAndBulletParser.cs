using System.Text;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using RssFeeder.Console.Models;

namespace RssFeeder.Console.Parsers
{
    class ParagraphAndBulletParser : IArticleParser
    {
        public string GetArticleBySelector(string html, SiteArticleDefinition options)
        {
            // Load and parse the html from the source file
            var parser = new HtmlParser();
            var document = parser.Parse(html);

            // Query the document by CSS selectors to get the article text
            var bullets = document.QuerySelectorAll(options.BulletSelector);    //".mol-bullets-with-font"
            StringBuilder description = new StringBuilder();
            BuildArticle(bullets, description);

            // "#js-article-text"
            // "p.mol-para-with-font"
            var paragraphs = document.QuerySelector(options.ArticleSelector).QuerySelectorAll(options.ParagraphSelector);
            BuildArticle(paragraphs, description);

            return description.ToString();
        }

        protected void BuildArticle(IHtmlCollection<IElement> elements, StringBuilder description)
        {
            foreach (var e in elements)
            {
                if (e.TagName.Equals("UL"))
                {
                    description.AppendLine($"<ul>{e.InnerHtml}</ul>");
                }
                else
                {
                    description.AppendLine($"<p>{e.TextContent.Trim()}</p>");
                }
            }
        }
    }
}
