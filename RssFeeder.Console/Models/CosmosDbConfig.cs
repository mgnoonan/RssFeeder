namespace RssFeeder.Console.Models;

public record CosmosDbConfig
{
    public string endpoint { get; init; }
    public string authKey { get; init; }
}
