namespace RssFeeder.Mvc.Services;

public class FeedDefinitionOptions
{
    public const string SectionName = "FeedDefinitions";

    public string SourceFile { get; set; } = "feeds.json";
}