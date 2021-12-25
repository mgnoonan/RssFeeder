namespace RssFeeder.Console.ArticleDefinitions;

public interface IArticleDefinitionFactory
{
    SiteArticleDefinition Get(string sitename);
}
