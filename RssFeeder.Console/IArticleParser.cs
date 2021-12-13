using Autofac;
using RssFeeder.Console.ArticleDefinitions;
using RssFeeder.Console.Utility;
using RssFeeder.Models;

namespace RssFeeder.Console
{
    public interface IArticleParser
    {
        void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory, IWebUtils webUtils);
        void Parse(RssFeedItem item);
    }
}
