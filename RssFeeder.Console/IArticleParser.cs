namespace RssFeeder.Console;

public interface IArticleParser
{
    void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory, IWebUtils webUtils);
    void Parse(RssFeedItem item);
}
