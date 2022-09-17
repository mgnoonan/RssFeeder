namespace RssFeeder.Console;

public interface IArticleParser
{
    void Initialize(IContainer container, IArticleDefinitionFactory definitionFactory, IWebUtils webUtils, ILogger log);
    void Parse(RssFeedItem item);
}
