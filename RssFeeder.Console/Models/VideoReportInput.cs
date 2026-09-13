namespace RssFeeder.Console.Models;

public record VideoReportInput
{
    [Description("The source collection/database name containing RssFeedItems")]
    public string CollectionName { get; init; } = "feed-items";
}
