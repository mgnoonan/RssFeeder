using RssFeeder.Console.Models;

namespace RssFeeder.Console.Parsers
{
    public interface ISiteParser
    {
        void Load(FeedItem item);
        void Parse();
        string GetArticleText(string html);
    }
}
