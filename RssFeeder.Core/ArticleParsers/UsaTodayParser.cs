using System.Text;
using AngleSharp.Dom;

namespace RssFeeder.Core.Parsers
{
    class UsaTodayParser : GenericParser
    {
        protected override string BuildArticleText(IHtmlCollection<IElement> paragraphs)
        {
            StringBuilder description = new StringBuilder();

            foreach (var p in paragraphs)
            {
                if (p.TagName.Equals("P"))
                {
                    if (p.TextContent.StartsWith("More:") || p.TextContent.StartsWith("Related:"))
                    {
                        // Skip
                    }
                    else
                    {
                        description.AppendLine($"<p>{p.TextContent.Trim()}</p>");
                    }
                }
                else
                {
                    description.AppendLine($"<h4>{p.TextContent.Trim()}</h4>");
                }
            }

            return description.ToString();
        }
    }
}
