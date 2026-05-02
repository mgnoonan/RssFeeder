namespace RssFeeder.Console;

public interface IArticleParser
{
    void Initialize(ITagParserResolver tagParserResolver, IArticleDefinitionFactory definitionFactory, IWebUtils webUtils, ILogger log);
    void Parse(RssFeedItem item);
}
