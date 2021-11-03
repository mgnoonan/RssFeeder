using Autofac;
using RssFeeder.Console.ArticleDefinitions;
using RssFeeder.Models;

namespace RssFeeder.Console
{
    public interface IArticleParser
    {
        void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory);
        void Parse(RssFeedItem item, RssFeed feed);
    }
}
