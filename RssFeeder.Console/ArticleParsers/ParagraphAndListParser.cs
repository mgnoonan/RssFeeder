using System.Text;
using AngleSharp.Dom;

namespace RssFeeder.Console.Parsers
{
    class ParagraphAndListParser : GenericParser
    {
        protected override string BuildArticleText(IHtmlCollection<IElement> paragraphs)
        {
            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                if (p.TagName.Equals("P"))
                {
                    description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
                }
                else
                {
                    description.AppendLine($"<ul>{p.InnerHtml}</ul>");
                }
            }

            return description.ToString();
        }
    }
}
