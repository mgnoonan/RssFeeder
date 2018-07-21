using RssFeeder.Console.Models;

namespace RssFeeder.Console.Parsers
{
    public interface ISiteParser
    {
        string GetArticleText(string html);
    }
}
